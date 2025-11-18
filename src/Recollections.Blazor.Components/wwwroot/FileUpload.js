// IndexedDB operations
const DB_NAME = 'FileUploadDB';
const DB_VERSION = 1;
const STORE_NAME = 'pendingUploads';

function initializeDB() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(DB_NAME, DB_VERSION);
        
        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve(request.result);
        
        request.onupgradeneeded = (event) => {
            const db = event.target.result;
            if (!db.objectStoreNames.contains(STORE_NAME)) {
                const store = db.createObjectStore(STORE_NAME, { keyPath: 'id', autoIncrement: true });
                store.createIndex('timestamp', 'timestamp', { unique: false });
                store.createIndex('entityType', 'entityType', { unique: false });
                store.createIndex('entityId', 'entityId', { unique: false });
            }
        };
    });
}

async function storeFiles(files, actionUrl, entityType, entityId) {
    const db = await initializeDB();
    const transaction = db.transaction([STORE_NAME], 'readwrite');
    const store = transaction.objectStore(STORE_NAME);
    
    const promises = Array.from(files).map(file => {
        return new Promise(async resolve => {
            const storedFile = {
                file: file,
                actionUrl: actionUrl,
                entityType: entityType,
                entityId: entityId,
                timestamp: Date.now()
            };

            const request = store.add(storedFile);
            request.onsuccess = () => {
                storedFile.id = request.result;
                resolve(storedFile);
            };
            request.onerror = () => {
                console.error('Failed to store file in IndexedDB:', request.error);
                storedFile.id = null;
                resolve(storedFile);
            };
        });
    });
    
    return Promise.all(promises);
}

async function getStoredFiles() {
    const db = await initializeDB();
    return new Promise((resolve, reject) => {
        const transaction = db.transaction([STORE_NAME], 'readonly');
        const store = transaction.objectStore(STORE_NAME);
        const request = store.getAll();
        
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

async function getStoredFilesByEntity(entityType, entityId) {
    const db = await initializeDB();
    return new Promise((resolve, reject) => {
        const transaction = db.transaction([STORE_NAME], 'readonly');
        const store = transaction.objectStore(STORE_NAME);
        const results = [];
        
        // Use cursor to filter by both entityType and entityId
        const request = store.openCursor();
        
        request.onsuccess = (event) => {
            const cursor = event.target.result;
            if (cursor) {
                const fileData = cursor.value;
                if (fileData.entityType === entityType && fileData.entityId === entityId) {
                    results.push(fileData);
                }
                cursor.continue();
            } else {
                resolve(results);
            }
        };
        
        request.onerror = () => reject(request.error);
    });
}

async function removeStoredFiles(id) {
    const db = await initializeDB();
    return new Promise((resolve, reject) => {
        const transaction = db.transaction([STORE_NAME], 'readwrite');
        const store = transaction.objectStore(STORE_NAME);
        const request = store.delete(id);
        
        request.onsuccess = () => resolve();
        request.onerror = () => reject(request.error);
    });
}

class EntityUploadQueue {
    constructor() {
        this.reset();
    }

    uploadError(statusCode, message) {
        this.progress[this.uploadIndex].status = "error";
        this.progress[this.uploadIndex].statusCode = statusCode;
        this.progress[this.uploadIndex].responseText = message;
        this.raiseProgress();
        this.uploadStep(null);
    }

    raiseProgress() {
        interop.invokeMethodAsync("FileUpload.OnChange", this.progress);
    }

    reset() {
        this.uploadIndex = -1;
        this.progress = [];
        this.storedFiles = [];
    }

    uploadCallback(imagesCount, imagesCompleted, responseText) {
        for (var i = 0; i < imagesCount; i++) {
            if (this.progress[i].status != "done" && this.progress[i].status != "error") {
                if (imagesCompleted > i) {
                    this.progress[i].status = "done";
                    this.progress[i].statusCode = 200;
                }

                if (imagesCompleted == i) {
                    this.progress[i].status = "current";
                    this.progress[i].uploaded = 0;
                } else if (imagesCompleted - 1 == i) {
                    if (responseText != null) {
                        this.progress[i].responseText = responseText;
                    }
                }
            }
        }

        this.raiseProgress();
    }

    uploadProgress(loaded, total) {
        interop.invokeMethodAsync("FileUpload.OnProgress", this.uploadIndex, total, loaded);
    }

    uploadStep(responseText) {
        this.uploadIndex++;
        this.uploadCallback(this.storedFiles.length, this.uploadIndex, responseText);

        if (this.storedFiles.length > this.uploadIndex) {
            const storedFile = this.storedFiles[this.uploadIndex];
            EntityUploadQueue.uploadFile(
                this.storedFiles[this.uploadIndex].file,
                storedFile.actionUrl,
                bearerToken,
                (response) => {
                    // Remove successfully uploaded file from IndexedDB
                    if (storedFile.id) {
                        removeStoredFiles(storedFile.id);
                    }

                    this.uploadStep(response);
                },
                (statusCode, message) => this.uploadError(statusCode, message),
                (loaded, total) => this.uploadProgress(loaded, total)
            );
        } else {
            this.reset();
        }
    }

    static uploadFile(file, url, bearerToken, onCompleted, onError, onProgress) {
        var formData = new FormData();
        formData.append("file", file, file.customName || file.name);

        var currentRequest = new XMLHttpRequest();
        currentRequest.onreadystatechange = function (e) {
            var request = e.target;

            if (request.readyState == XMLHttpRequest.DONE) {
                if (request.status == 200) {
                    var responseText = currentRequest.responseText;
                    onCompleted(responseText);
                }
                else if (request.status != 0 && onError != null) {
                    onError(currentRequest.status, currentRequest.statusText);
                }
            }
        };

        if (onError != null) {
            currentRequest.onerror = function (e) {
                onError(500, e.message);
            };
        }

        if (onProgress != null) {
            currentRequest.upload.onprogress = function (e) {
                onProgress(e.loaded, e.total);
            };
        }

        currentRequest.open("POST", url);

        if (bearerToken != null) {
            currentRequest.setRequestHeader("Authorization", "Bearer " + bearerToken);
        }

        currentRequest.send(formData);
    }

    async storeAndQueueFiles(items, actionUrl, entityType, entityId) {
        const storedItems = await storeFiles(items, actionUrl, entityType, entityId);
        this.addStoredFilesToQueue(storedItems);
    }

    addStoredFilesToQueue(storedItems) {
        // Add stored files from IndexedDB to queue
        for (var i = 0; i < storedItems.length; i++) {
            var item = storedItems[i];
            this.storedFiles.push(item);
            this.progress.push({
                id: item.id ? `${item.id}` : null,
                entityType: item.entityType,
                entityId: item.entityId,
                status: "pending",
                statusCode: 0,
                name: item.file.name,
                responseText: null,
                uploaded: 0,
                size: item.file.size
            });
        }

        if (this.uploadIndex == -1) {
            this.uploadStep();
        }
    }
}

const queue = new EntityUploadQueue();
let interop;
let bearerToken;

export function initialize(interopValue) {
    interop = interopValue;
}

export function setBearerToken(bearerTokenValue) {
    bearerToken = bearerTokenValue;
}

export function bindForm(entityType, entityId, url, form, dragAndDropContainer) {
    form = $(form);

    if (form.data('fileUpload') != null)
        return;

    form.data('fileUpload', queue);

    var input = form.find("input[type=file]");

    form.find("button").click(function (e) {
        input.click();
        e.preventDefault();
    });
    input.change(async () => {
        await queue.storeAndQueueFiles(input[0].files, url, entityType, entityId);
        form[0].reset();
    });

    if (dragAndDropContainer) {
        dragAndDropContainer.addEventListener('drag', function (e) {
            e.preventDefault();
        });
        dragAndDropContainer.addEventListener('dragstart', function (e) {
            e.preventDefault();
        });
        dragAndDropContainer.addEventListener('dragend', function (e) {
            e.preventDefault();
        });
        dragAndDropContainer.addEventListener('dragover', function (e) {
            e.preventDefault();
        });
        dragAndDropContainer.addEventListener('dragenter', function (e) {
            e.preventDefault();
        });
        dragAndDropContainer.addEventListener('dragleave', function (e) {
            e.preventDefault();
        });
        dragAndDropContainer.addEventListener('drop', function (e) {
            queue.storeAndQueueFiles(e.dataTransfer.files, url, entityType, entityId);
            e.preventDefault();
        });
    }
}

export async function getEntityStoredFiles(entityType, entityId) {
    const storedFiles = await getStoredFilesByEntity(entityType, entityId);
    queue.progress.forEach(p => {
        if (p.entityType !== entityType || p.entityId !== entityId)
            return;

        const index = storedFiles.findIndex(f => f.file.name === p.name);
        if (index >= 0) {
            storedFiles.removeAt(index);
        }
    });
    return storedFiles.map(f => { return { name: f.file.name, size: f.file.size, id: `${f.id}` }; });
}

export async function retryEntityQueue(entityType, entityId) {
    const storedFiles = await getStoredFilesByEntity(entityType, entityId);
    if (storedFiles.length > 0) {
        queue.addStoredFilesToQueue(storedFiles);
    }
}

export async function clearEntityQueue(entityType, entityId) {
    const storedFiles = await getStoredFilesByEntity(entityType, entityId);
    if (storedFiles.length > 0) {
        await Promise.all(storedFiles.map(f => removeStoredFiles(f.id)));
    }
}

export function deleteFile(id) {
    return removeStoredFiles(Number.parseInt(id));
}

export function destroy() {

}
