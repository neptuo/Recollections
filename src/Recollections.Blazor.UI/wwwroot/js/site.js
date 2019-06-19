window.Bootstrap = {
    Modal: {
        Register: function (id) {
            var target = $("#" + id);
            target.on('shown.bs.modal', function (e) {
                $(e.currentTarget).find('[data-autofocus]').select().focus();
            });
            target.on('hidden.bs.modal', function (e) {
                DotNet.invokeMethodAsync("Recollections.Blazor.Components", "Bootstrap_ModalHidden", e.currentTarget.id);
            });

            return true;
        },
        Toggle: function (id, isVisible) {
            var target = $("#" + id);
            target.modal(isVisible ? 'show' : 'hide');

            return true;
        }
    }
};

window.Recollections = {
    NavigateTo: function (href) {
        window.location.href = href;
        return true;
    },
    SaveToken: function (token) {
        if ("localStorage" in window) {
            if (token == null)
                window.localStorage.removeItem("token");
            else
                window.localStorage.setItem("token", token);
        }
    },
    LoadToken: function () {
        if ("localStorage" in window) {
            return window.localStorage.getItem("token");
        }

        return null;
    }
};

window.FileUpload = {
    Initialize: function (formId, bearerToken) {
        var form = $("#" + formId);
        var input = form.find("input[type=file]");

        var uploadIndex = -1;
        function uploadStep() {
            uploadIndex++;

            var files = input[0].files;
            if (files.length > uploadIndex) {
                FileUpload.UploadFile(files[uploadIndex], form[0].action, bearerToken, uploadStep, uploadStep);
            }
            else {
                uploadIndex = -1;
                form[0].reset();
                DotNet.invokeMethodAsync("Recollections.Blazor.Components", "FileUpload_OnCompleted", formId);
            }
        }

        form.find("button").click(function (e) {
            input.click();
            e.preventDefault();
        });
        input.change(function () {
            uploadStep();
        });
    },
    UploadFile: function(file, url, bearerToken, onCompleted, onError, onProgress) {
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
                else if (onError != null) {
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
}