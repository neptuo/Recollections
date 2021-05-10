
export function initialize(interop, form, bearerToken, isDragAndDrop) {
    form = $(form);

    if (form.data('fileUpload') != null)
        return;

    var fileUpload = {};
    form.data('fileUpload', fileUpload);

    var input = form.find("input[type=file]");

    var uploadIndex = -1;
    var progress = [];
    var files = [];

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
            uploadFile(
                files[uploadIndex],
                form[0].action,
                bearerToken,
                uploadStep,
                uploadError,
                uploadProgress
            );
        } else {
            resetForm();
        }
    }

    function addFilesToQueue(items) {
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
    }

    form.find("button").click(function (e) {
        input.click();
        e.preventDefault();
    });
    input.change(function () {
        addFilesToQueue(input[0].files);

        if (uploadIndex == -1) {
            uploadStep();
        }
    });

    if (isDragAndDrop) {
        document.body.addEventListener('drag', function (e) {
            e.preventDefault();
        });
        document.body.addEventListener('dragstart', function (e) {
            e.preventDefault();
        });
        document.body.addEventListener('dragend', function (e) {
            e.preventDefault();
        });
        document.body.addEventListener('dragover', function (e) {
            e.preventDefault();
        });
        document.body.addEventListener('dragenter', function (e) {
            e.preventDefault();
        });
        document.body.addEventListener('dragleave', function (e) {
            e.preventDefault();
        });
        document.body.addEventListener('drop', function (e) {
            addFilesToQueue(e.dataTransfer.files);

            if (uploadIndex == -1) {
                uploadStep();
            }

            e.preventDefault();
        });
    }
};

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
