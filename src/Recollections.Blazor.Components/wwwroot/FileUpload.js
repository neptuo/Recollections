// Sync with FileUpload.js
async function storeFiles(files, actionUrl, entityType, entityId, userId) {
    const mediaCache = await caches.open('media');
    
    const promises = Array.from(files).map(file => {
        return new Promise(async resolve => {
            let id = self.crypto.randomUUID();
            const request = new Request(id);
            const result = await mediaCache.put(request, new Response(file, { 
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
            if (entityType != null && entityId != null && uId == userId) {
                isPassed = true;
            }
        } else {
            if (entityType == '' && entityId == '' && uId == '') {
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

export async function removeStoredFile(id) {
    const mediaCache = await caches.open('media');
    mediaCache.delete(id);
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
                        removeStoredFile(storedFile.id);
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

export function initialize(interopValue) {
    interop = interopValue;
}

export function setBearerToken(userIdValue, bearerTokenValue) {
    userId = userIdValue;
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
    return storedFiles.map(f => { return { name: f.file.name, size: f.file.size, id: `${f.id}` }; });
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
        await Promise.all(storedFiles.map(f => removeStoredFile(f.id)));
    }
}

export async function uploadUnassignedFilesTo(entityType, entityId, url) {
    const unassignedFiles = await getStoredFilesByFlag(false);
    if (!unassignedFiles || unassignedFiles.length === 0) {
        return;
    }

    const items = unassignedFiles.map(f => f.file);
    queue.storeAndQueueFiles(items, url, entityType, entityId);

    await Promise.all(unassignedFiles.map(f => removeStoredFile(f.id)));
}

export function destroy() {

}