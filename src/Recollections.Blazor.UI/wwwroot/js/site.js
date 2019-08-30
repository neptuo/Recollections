﻿window.Bootstrap = {
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

window.Map = {
    Initialize: function (container, interop, zoom, isEditable, markers) {
        var isInitialization = false;
        var model = null;

        $container = $(container);
        if ($container.data('map') == null) {
            isInitialization = true;

            var map = new SMap(container);
            map.addDefaultLayer(SMap.DEF_BASE).enable();
            map.addDefaultControls();
            map.setZoom(zoom);

            var layer = new SMap.Layer.Marker();
            map.addLayer(layer).enable();

            model = {
                map: map,
                layer: layer,
                interop: interop,
                isEditable: isEditable,
                isEmptyPoint: false
            };
            $container.data('map', model);

            if (isEditable) {
                function dragStart(e) {
                    var node = e.target.getContainer();
                    node[SMap.LAYER_MARKER].style.cursor = "help";
                }

                function dragStop(e) {
                    var node = e.target.getContainer();
                    node[SMap.LAYER_MARKER].style.cursor = "";
                    var coords = e.target.getCoords();

                    var id = Number.parseInt(e.target.getId());
                    moverMarkerOnCoords(id, coords);
                }

                function click(e) {
                    if (model.isEmptyPoint) {
                        var coords = SMap.Coords.fromEvent(e.data.event, map);
                        moverMarkerOnCoords(0, coords);
                    }
                }

                function moverMarkerOnCoords(id, coords) {
                    var latitude = coords.y;
                    var longitude = coords.x;

                    coords.getAltitude().then(function (altitude) {
                        interop.invokeMethodAsync("MarkerMoved", id, latitude, longitude, altitude);
                    });
                }

                var signals = map.getSignals();
                signals.addListener(window, "marker-drag-start", dragStart);
                signals.addListener(window, "marker-drag-stop", dragStop);
                signals.addListener(window, "map-click", click);
            }
        }

        model = $container.data('map');
        var points = Map.SetMarkers(model, markers);

        model.isEmptyPoint = points.length == 0;
        if (model.isEmptyPoint) {
            model.map.setCursor("pointer");
            if (isInitialization) {
                model.map.setZoom(1);
            }
        } else {
            model.map.setCursor(null);
            if (isInitialization) {
                var centerZoom = model.map.computeCenterZoom(points);
                model.map.setCenterZoom(centerZoom[0], centerZoom[1]);
            }
        }
    },
    SetMarkers: function (model, markers) {
        model.layer.removeAll();
        var points = [];
        for (var i = 0; i < markers.length; i++) {
            if (markers[i].longitude == null && markers[i].latitude == null) {
                continue;
            }

            var options = {};
            var point = SMap.Coords.fromWGS84(markers[i].longitude, markers[i].latitude);
            var marker = new SMap.Marker(point, "" + i, options);

            if (model.isEditable) {
                marker.decorate(SMap.Marker.Feature.Draggable);
            }

            model.layer.addMarker(marker);

            points.push(point);
        }

        return points;
    }
};