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

async function storeFilesInDB(files, actionUrl, entityType, entityId) {
    const db = await initializeDB();
    const transaction = db.transaction([STORE_NAME], 'readwrite');
    const store = transaction.objectStore(STORE_NAME);
    
    const promises = Array.from(files).map(file => {
        return new Promise((resolve, reject) => {
            const fileData = {
                file: file,
                actionUrl: actionUrl,
                entityType: entityType,
                entityId: entityId,
                timestamp: Date.now()
            };
            
            const request = store.add(fileData);
            request.onsuccess = () => {
                fileData.id = request.result;
                resolve(fileData);
            };
            request.onerror = () => reject(request.error);
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

async function removeFileFromDB(id) {
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
    constructor(interop, bearerToken) {
        this.interop = interop;
        this.bearerToken = bearerToken;
        
        this.uploadIndex = -1;
        this.progress = [];
        this.files = [];
        this.storedFileIds = [];
        this.storedFileData = [];
    }

    uploadError(statusCode, message) {
        this.progress[this.uploadIndex].status = "error";
        this.progress[this.uploadIndex].statusCode = statusCode;
        this.progress[this.uploadIndex].responseText = message;
        this.raiseProgress();
        this.uploadStep(null);
    }

    raiseProgress() {
        this.interop.invokeMethodAsync("FileUpload.OnProgress", this.progress);
    }

    resetForm() {
        this.uploadIndex = -1;
        this.progress = [];
        this.files = [];
        this.storedFileIds = [];
        this.storedFileData = [];
    }

    uploadCallback(imagesCount, imagesCompleted, currentSize, currentUploaded, responseText) {
        for (var i = 0; i < imagesCount; i++) {
            if (this.progress[i].status != "done" && this.progress[i].status != "error") {
                if (imagesCompleted > i) {
                    this.progress[i].status = "done";
                    this.progress[i].statusCode = 200;
                }

                if (imagesCompleted == i) {
                    this.progress[i].status = "current";
                    this.progress[i].uploaded = currentUploaded;
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
        this.uploadCallback(this.files.length, this.uploadIndex, total, loaded, null);
    }

    uploadStep(responseText) {
        this.uploadIndex++;
        this.uploadCallback(this.files.length, this.uploadIndex, 0, 0, responseText);

        if (this.files.length > this.uploadIndex) {
            const fileData = this.storedFileData[this.uploadIndex];
            EntityUploadQueue.uploadFile(
                this.files[this.uploadIndex],
                fileData.actionUrl,
                this.bearerToken,
                (response) => {
                    // Remove successfully uploaded file from IndexedDB
                    if (this.storedFileIds[this.uploadIndex]) {
                        removeFileFromDB(this.storedFileIds[this.uploadIndex]);
                    }
                    this.uploadStep(response);
                },
                (statusCode, message) => this.uploadError(statusCode, message),
                (loaded, total) => this.uploadProgress(loaded, total)
            );
        } else {
            this.resetForm();
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

    async storeAndQueueFiles(items, url, entityType, entityId) {
        try {
            // Store files in IndexedDB first
            const storedItems = await storeFilesInDB(items, url, entityType, entityId);
            
            this.addStoredFilesToQueue(storedItems);
        } catch (error) {
            console.error('Failed to store files in IndexedDB:', error);
            // Fallback to original behavior
            for (var i = 0; i < items.length; i++) {
                var file = items[i];
                this.files.push(file);
                this.progress.push({
                    entityType: entityType,
                    entityId: entityId,
                    status: "pending",
                    statusCode: 0,
                    name: file.name,
                    responseText: null,
                    uploaded: 0,
                    size: file.size
                });
            }

            if (this.uploadIndex == -1) {
                this.uploadStep();
            }
        }
    }

    addStoredFilesToQueue(storedItems) {
        // Add stored files from IndexedDB to queue
        for (var i = 0; i < storedItems.length; i++) {
            var item = storedItems[i];
            this.storedFileIds.push(item.id);
            this.storedFileData.push(item);
            this.files.push(item.file);
            this.progress.push({
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

const data = new Map();

export function bindForm(interop, entityType, entityId, url, bearerToken, form, dragAndDropContainer) {
    form = $(form);

    if (form.data('fileUpload') != null)
        return;

    const state = new EntityUploadQueue(interop, bearerToken);
    data.set(entityType + "_" + entityId, state);
    form.data('fileUpload', state);

    var input = form.find("input[type=file]");

    form.find("button").click(function (e) {
        input.click();
        e.preventDefault();
    });
    input.change(async () => {
        await state.storeAndQueueFiles(input[0].files, url, entityType, entityId);
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
            state.storeAndQueueFiles(e.dataTransfer.files, url, entityType, entityId);
            e.preventDefault();
        });
    }
}

export async function getEntityStoredFiles(entityType, entityId) {
    const storedFiles = await getStoredFilesByEntity(entityType, entityId);
    const queue = data.get(entityType + "_" + entityId);
    if (queue) {
        queue.progress.forEach(p => {
            const index = storedFiles.findIndex(f => f.file.name === p.name);
            if (index >= 0) {
                storedFiles.removeAt(index);
            }
        });
    }
    return storedFiles.map(f => { return { name: f.file.name, size: f.file.size, id: `${f.id}` }; });
}

export async function retryEntityQueue(entityType, entityId) {
    const storedFiles = await getStoredFilesByEntity(entityType, entityId);
    if (storedFiles.length > 0) {
        const state = data.get(entityType + "_" + entityId);
        if (state) {
            state.addStoredFilesToQueue(storedFiles);
        }
    }
}

export async function clearEntityQueue(entityType, entityId) {
    const storedFiles = await getStoredFilesByEntity(entityType, entityId);
    if (storedFiles.length > 0) {
        await Promise.all(storedFiles.map(f => removeFileFromDB(f.id)));
    }
}

export function deleteFile(id) {
    return removeFileFromDB(Number.parseInt(id));
}

export function destroy() {

}
