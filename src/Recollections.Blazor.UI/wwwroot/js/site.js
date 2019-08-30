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
    Initialize: function (interop, form, bearerToken) {
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
            interop.invokeMethodAsync("OnCompleted", progress);
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
                FileUpload.UploadFile(
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

        form.find("button").click(function (e) {
            input.click();
            e.preventDefault();
        });
        input.change(function () {
            for (var i = 0; i < input[0].files.length; i++) {
                var file = input[0].files[i];
                files.push(file);
                progress.push({
                    status: "pending",
                    statusCode: 0,
                    responseText: null,
                    uploaded: 0,
                    size: file.size
                });
            }

            if (uploadIndex == -1) {
                uploadStep();
            }
        });
    },
    UploadFile: function (file, url, bearerToken, onCompleted, onError, onProgress) {
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
};

window.InlineMarkdownEdit = {
    editors: {},
    Initialize: function (textAreaId) {
        if (InlineMarkdownEdit.editors[textAreaId] != null) {
            return;
        }

        var editor = new EasyMDE({
            element: document.getElementById(textAreaId),
            autofocus: true,
            forceSync: true,
            spellChecker: false,
            toolbar: [
                "heading-2",
                "heading-3",
                "|",
                "bold",
                "italic",
                "|",
                "unordered-list",
                "ordered-list",
                "|",
                "link",
                "quote",
                "horizontal-rule",
                {
                    name: "cancel",
                    className: "fa fa-times pull-right",
                    title: "Close Editor",
                    action: function (editor) {
                        DotNet.invokeMethodAsync("Recollections.Blazor.Components", "InlineMarkdownEdit_OnCancel", textAreaId);
                    }
                },
                {
                    name: "save",
                    className: "fa fa-check pull-right",
                    title: "Save",
                    action: function (editor) {
                        var value = editor.value();
                        DotNet.invokeMethodAsync("Recollections.Blazor.Components", "InlineMarkdownEdit_OnSave", textAreaId, value);
                    }
                },
            ],
            shortcuts: {
                "save": "Ctrl-Enter",
                "cancel": "Escape"
            }
        });
        InlineMarkdownEdit.editors[textAreaId] = editor;
    },
    Destroy: function (textAreaId) {
        if (InlineMarkdownEdit.editors[textAreaId] != null) {
            InlineMarkdownEdit.editors[textAreaId].toTextArea();
            InlineMarkdownEdit.editors[textAreaId] = null;
        }
    },
    SetValue: function (textAreaId, value) {
        if (InlineMarkdownEdit.editors[textAreaId] != null) {
            if (value == null) {
                value = "";
            }

            return InlineMarkdownEdit.editors[textAreaId].value(value);
        }
    },
    GetValue: function (textAreaId) {
        if (InlineMarkdownEdit.editors[textAreaId] != null) {
            return InlineMarkdownEdit.editors[textAreaId].value();
        }
    }
}

window.InlineTextEdit = {
    Initialize: function (inputId) {
        $('#' + inputId).focus().keyup(function (e) {
            if (e.keyCode == 27) {
                $(this).blur();
                setTimeout(function () {
                    DotNet.invokeMethodAsync("Recollections.Blazor.Components", "InlineTextEdit_OnCancel", inputId);
                }, 1);
            }
        });
    }
};

window.InlineDateEdit = {
    Initialize: function (inputId, format) {
        $('#' + inputId).datepicker({
            format: format.toLowerCase(),
            autoclose: true,
            todayHighlight: true,
            todayBtn: "linked"
        });
    },
    Destroy: function (inputId) {
        $('#' + inputId).datepicker("destroy");
    },
    GetValue: function (inputId) {
        return $('#' + inputId).val();
    }
};

window.DatePicker = {
    Initialize: function (inputId, format) {
        $('#' + inputId).datepicker({
            format: format.toLowerCase(),
            autoclose: true,
            todayHighlight: true,
            todayBtn: "linked"
        });
    },
    Destroy: function (inputId) {
        $('#' + inputId).datepicker("destroy");
    },
    GetValue: function (inputId) {
        return $('#' + inputId).val();
    }
}

window.Downloader = {
    FromUrlAsync: function (name, url) {
        var link = document.createElement("a");
        link.target = "_blank";
        link.download = name;
        link.href = url;
        link.click();
    }
};