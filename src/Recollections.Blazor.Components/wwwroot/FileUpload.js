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
            }
        };
    });
}

async function storeFilesInDB(files, actionUrl, entityType, entityId) {
    const db = await initializeDB();
    const transaction = db.transaction([STORE_NAME], 'readwrite');
    const store = transaction.objectStore(STORE_NAME);
    
    const fileDataArray = [];
    const promises = Array.from(files).map(file => {
        return new Promise((resolve, reject) => {
            const fileData = {
                file: file,
                actionUrl: actionUrl,
                entityType: entityType,
                entityId: entityId,
                timestamp: Date.now()
            };
            
            fileDataArray.push(fileData);
            
            const request = store.add(fileData);
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    });
    
    const ids = await Promise.all(promises);
    return { ids, fileDataArray };
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

export function initialize(interop, form, bearerToken, dragAndDropTarget, entityType, entityId) {
    form = $(form);

    if (form.data('fileUpload') != null)
        return;

    var fileUpload = {};
    form.data('fileUpload', fileUpload);

    var input = form.find("input[type=file]");

    var uploadIndex = -1;
    var progress = [];
    var files = [];
    var storedFileIds = [];
    var storedFileData = [];

    function uploadError(statusCode, message) {
        progress[uploadIndex].status = "error";
        progress[uploadIndex].statusCode = statusCode;
        progress[uploadIndex].responseText = message;
        raiseProgress();
        uploadStep(null);
    }

    function raiseProgress() {
        interop.invokeMethodAsync("FileUpload.OnCompleted", progress);
    }

    function resetForm() {
        uploadIndex = -1;
        progress = [];
        files = [];
        storedFileIds = [];
        storedFileData = [];
        form[0].reset();
    }

    function uploadCallback(imagesCount, imagesCompleted, currentSize, currentUploaded, responseText) {
        for (var i = 0; i < imagesCount; i++) {
            if (progress[i].status != "done" && progress[i].status != "error") {
                if (imagesCompleted > i) {
                    progress[i].status = "done";
                    progress[i].statusCode = 200;
                }

                if (imagesCompleted == i) {
                    progress[i].status = "current";
                    progress[i].uploaded = currentUploaded;
                } else if (imagesCompleted - 1 == i) {
                    if (responseText != null) {
                        progress[i].responseText = responseText;
                    }
                }
            }
        }

        raiseProgress();
    }

    function uploadProgress(loaded, total) {
        uploadCallback(input[0].files.length, uploadIndex, total, loaded, null);
    }

    function uploadStep(responseText) {
        uploadIndex++;
        uploadCallback(files.length, uploadIndex, 0, 0, responseText);

        if (files.length > uploadIndex) {
            const fileData = storedFileData[uploadIndex];
            uploadFile(
                files[uploadIndex],
                fileData.actionUrl,
                bearerToken,
                (response) => {
                    // Remove successfully uploaded file from IndexedDB
                    if (storedFileIds[uploadIndex]) {
                        removeFileFromDB(storedFileIds[uploadIndex]);
                    }
                    uploadStep(response);
                },
                uploadError,
                uploadProgress
            );
        } else {
            resetForm();
        }
    }

    async function addFilesToQueue(items, skipIndexedDB = false) {
        if (!skipIndexedDB) {
            try {
                // Store files in IndexedDB first
                const { ids, fileDataArray } = await storeFilesInDB(items, form[0].action, entityType, entityId);
                storedFileIds.push(...ids);
                storedFileData.push(...fileDataArray);
                
                for (var i = 0; i < items.length; i++) {
                    var file = items[i];
                    files.push(file);
                    progress.push({
                        status: "pending",
                        statusCode: 0,
                        name: file.name,
                        responseText: null,
                        uploaded: 0,
                        size: file.size
                    });
                }

                if (uploadIndex == -1) {
                    uploadStep();
                }
            } catch (error) {
                console.error('Failed to store files in IndexedDB:', error);
                // Fallback to original behavior
                for (var i = 0; i < items.length; i++) {
                    var file = items[i];
                    files.push(file);
                    progress.push({
                        status: "pending",
                        statusCode: 0,
                        name: file.name,
                        responseText: null,
                        uploaded: 0,
                        size: file.size
                    });
                }

                if (uploadIndex == -1) {
                    uploadStep();
                }
            }
        } else {
            // Skip IndexedDB storage and use items as stored file data
            for (var i = 0; i < items.length; i++) {
                var item = items[i];
                storedFileIds.push(item.id);
                storedFileData.push(item);
                files.push(item.file);
                progress.push({
                    status: "pending",
                    statusCode: 0,
                    name: item.file.name,
                    responseText: null,
                    uploaded: 0,
                    size: item.file.size
                });
            }

            if (uploadIndex == -1) {
                uploadStep();
            }
        }
    }

    // Initialize by checking for existing files in IndexedDB
    async function initializeStoredFiles() {
        try {
            const storedFiles = await getStoredFiles();
            const filteredFiles = storedFiles.filter(fileData => 
                fileData.entityType === entityType && fileData.entityId === entityId
            );
            
            if (filteredFiles.length > 0) {
                if (confirm(`You have ${filteredFiles.length} pending file uploads. Do you want to resume uploading them?`)) {
                    await addFilesToQueue(filteredFiles, true);
                } else {
                    await Promise.all(filteredFiles.map(f => removeFileFromDB(f.id)));
                }
            }
        } catch (error) {
            console.error('Failed to retrieve stored files from IndexedDB:', error);
        }
    }

    // Start initialization
    initializeStoredFiles();

    form.find("button").click(function (e) {
        input.click();
        e.preventDefault();
    });
    input.change(function () {
        addFilesToQueue(input[0].files);
    });

    if (dragAndDropTarget) {
        dragAndDropTarget.addEventListener('drag', function (e) {
            e.preventDefault();
        });
        dragAndDropTarget.addEventListener('dragstart', function (e) {
            e.preventDefault();
        });
        dragAndDropTarget.addEventListener('dragend', function (e) {
            e.preventDefault();
        });
        dragAndDropTarget.addEventListener('dragover', function (e) {
            e.preventDefault();
        });
        dragAndDropTarget.addEventListener('dragenter', function (e) {
            e.preventDefault();
        });
        dragAndDropTarget.addEventListener('dragleave', function (e) {
            e.preventDefault();
        });
        dragAndDropTarget.addEventListener('drop', function (e) {
            addFilesToQueue(e.dataTransfer.files);
            e.preventDefault();
        });
    }
}

export function destroy() {

}

function uploadFile(file, url, bearerToken, onCompleted, onError, onProgress) {
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
