// Sync with share-target.js (1:1)
async function storeFiles(files, actionUrl, entityType, entityId, userId) {
    const mediaCache = await caches.open('media');
    
    const promises = Array.from(files).map(file => {
        return new Promise(async resolve => {
            let id = self.crypto.randomUUID();
            const request = new Request(id);
            await mediaCache.put(request, new Response(file, { 
                headers: { 
                    'X-Entity-Type': entityType || '',
                    'X-Entity-Id': entityId || '',
                    'X-User-Id': userId || '',
                    'X-Action-Url': actionUrl || '',

                    'X-File-Name': file.name,
                    'X-Last-Modified': file.lastModified,
                    'Content-Size': file.size,
                    'Content-Type': file.type,
                }
            }));
            resolve({
                id: request.url,
                file: file,
                actionUrl: actionUrl,
                userId: userId,
                entityType: entityType,
                entityId: entityId,
            });
        });
    });

    return Promise.all(promises);
}

async function getStoredFilesByFlag(assigned) {
    const mediaCache = await caches.open('media');
    const storedFiles = [];
    const requests = await mediaCache.keys();
    for (const request of requests) {
        const response = await mediaCache.match(request);
        const entityType = response.headers.get('X-Entity-Type');
        const entityId = response.headers.get('X-Entity-Id');
        const actionUrl = response.headers.get('X-Action-Url');
        const uId = response.headers.get('X-User-Id');
        let isPassed = false;
        if (assigned) {
            if (entityType && entityId && uId == userId) {
                isPassed = true;
            }
        } else {
            if (!entityType && !entityId && (!uId || uId == userId)) {
                isPassed = true;
            }
        }

        if (isPassed) {
            const file = await response.blob();
            file.name = response.headers.get('X-File-Name');
            file.lastModified = Number.parseInt(response.headers.get('X-Last-Modified'));
            
            const storedFile = {
                id: request.url,
                file: file,
                actionUrl: actionUrl,
                userId: userId,
                entityType: entityType,
                entityId: entityId,
            };
            storedFiles.push(storedFile);
        }
    }

    return storedFiles;
}

class EntityUploadQueue {
    constructor() {
        this.reset();
    }

    uploadError(statusCode, message) {
        this.progress[this.uploadIndex].status = "error";
        this.progress[this.uploadIndex].statusCode = statusCode;
        this.progress[this.uploadIndex].responseText = message;
        this.raiseChanged();
        this.uploadStep(null);
    }

    raiseChanged() {
        interop.invokeMethodAsync("FileUpload.OnChange", this.progress);
    }

    raiseProgress(loaded, total) {
        interop.invokeMethodAsync("FileUpload.OnProgress", this.uploadIndex, total, loaded);
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

        this.raiseChanged();
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
                        removeStoredFileInternal(storedFile.id, false);
                    }

                    this.uploadStep(response);
                },
                (statusCode, message) => this.uploadError(statusCode, message),
                (loaded, total) => this.raiseProgress(loaded, total)
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
        const storedItems = await storeFiles(items, actionUrl, entityType, entityId, userId);
        if (!actionUrl || !entityType || !entityId) {
            raiseStoredFilesChanged();
            return;
        }

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
        } else {
            this.raiseChanged();
        }
    }
}

const queue = new EntityUploadQueue();
let interop;
let userId;
let bearerToken;
let currentEntityType;
let currentEntityId;
let currentActionUrl;
let isDropTargetBound = false;

function hasFiles(dataTransfer) {
    return dataTransfer && Array.from(dataTransfer.types || []).includes('Files');
}

function raiseStoredFilesChanged() {
    if (interop) {
        interop.invokeMethodAsync("FileUpload.OnStoredFilesChanged");
    }
}

function ensureDropTargetBinding() {
    if (isDropTargetBound) {
        return;
    }

    const dropTarget = document.body || document.documentElement;
    if (!dropTarget) {
        return;
    }

    const preventDefault = function (e) {
        if (hasFiles(e.dataTransfer)) {
            e.preventDefault();
        }
    };

    dropTarget.addEventListener('drag', preventDefault);
    dropTarget.addEventListener('dragstart', preventDefault);
    dropTarget.addEventListener('dragend', preventDefault);
    dropTarget.addEventListener('dragover', preventDefault);
    dropTarget.addEventListener('dragenter', preventDefault);
    dropTarget.addEventListener('dragleave', preventDefault);
    dropTarget.addEventListener('drop', function (e) {
        if (!hasFiles(e.dataTransfer)) {
            return;
        }

        queue.storeAndQueueFiles(
            e.dataTransfer.files,
            currentActionUrl,
            currentEntityType,
            currentEntityId
        );
        e.preventDefault();
    });

    isDropTargetBound = true;
}

export function initialize(interopValue) {
    interop = interopValue;
}

export function setBearerToken(userIdValue, bearerTokenValue) {
    userId = userIdValue;
    bearerToken = bearerTokenValue;
}

export function setCurrentEntity(entityTypeValue, entityIdValue, actionUrlValue) {
    currentEntityType = entityTypeValue;
    currentEntityId = entityIdValue;
    currentActionUrl = actionUrlValue;
}

const _formData = new WeakMap();

export function bindForm(entityType, entityId, url, form, dragAndDropContainer) {
    if (_formData.has(form))
        return;

    _formData.set(form, queue);

    var input = form.querySelector("input[type=file]");

    input.addEventListener('change', async () => {
        await queue.storeAndQueueFiles(
            input.files,
            url || currentActionUrl,
            entityType || currentEntityType,
            entityId || currentEntityId
        );
        form.reset();
    });

    if (dragAndDropContainer) {
        ensureDropTargetBinding();
    }
}

export function openFileDialog(form) {
    var input = form?.querySelector("input[type=file]");
    if (input) {
        input.click();
    }
}

export async function getStoredFiles() {
    const storedFiles = await getStoredFilesByFlag(true);
    queue.progress.forEach(p => {
        const index = storedFiles.findIndex(f => f?.file.name === p.name);
        if (index >= 0) {
            delete storedFiles[index];
        }
    });
    return storedFiles.filter(f => f != null).map(f => { return { name: f.file.name, size: f.file.size, id: `${f.id}` }; });
}

export async function getUnassignedSharedFiles() {
    const storedFiles = await getStoredFilesByFlag(false);
    return storedFiles.map(f => {
        const contentType = f.file.type || '';
        const previewUrl = contentType.startsWith('image/') || contentType.startsWith('video/')
            ? URL.createObjectURL(f.file)
            : null;

        return {
            name: f.file.name,
            size: f.file.size,
            id: `${f.id}`,
            contentType: contentType,
            previewUrl: previewUrl
        };
    });
}

export async function retryStoredFiles(ids) {
    let storedFiles = await getStoredFilesByFlag(true);
    if (ids && ids.length > 0) {
        storedFiles = storedFiles.filter(f => ids.includes(`${f.id}`));
    }
    if (storedFiles.length > 0) {
        queue.addStoredFilesToQueue(storedFiles);
    }
}

export async function clearStoredFiles(ids) {
    let storedFiles = await getStoredFilesByFlag(true);
    if (ids && ids.length > 0) {
        storedFiles = storedFiles.filter(f => ids.includes(`${f.id}`));
    }
    if (storedFiles.length > 0) {
        await Promise.all(storedFiles.map(f => removeStoredFileInternal(f.id, false)));
        raiseStoredFilesChanged();
    }
}

export async function uploadUnassignedFilesTo(entityType, entityId, url) {
    const unassignedFiles = await getStoredFilesByFlag(false);
    if (!unassignedFiles || unassignedFiles.length === 0) {
        return;
    }

    const items = unassignedFiles.map(f => f.file);
    await queue.storeAndQueueFiles(items, url, entityType, entityId);

    await Promise.all(unassignedFiles.map(f => removeStoredFileInternal(f.id, false)));
    raiseStoredFilesChanged();
}

export function destroy() {

}

async function removeStoredFileInternal(id, shouldNotify) {
    const mediaCache = await caches.open('media');
    await mediaCache.delete(id);

    if (shouldNotify) {
        raiseStoredFilesChanged();
    }
}

export async function removeStoredFile(id) {
    await removeStoredFileInternal(id, true);
}
