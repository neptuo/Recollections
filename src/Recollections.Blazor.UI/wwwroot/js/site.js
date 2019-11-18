window.Bootstrap = {
    Modal: {
        Show: function (container) {
            $(container).modal('show');
        },
        Hide: function (container) {
            $(container).modal('hide');
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
                    name: file.name,
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
    Initialize: function (container, interop, markers, isZoomed, isResizable) {
        var isInitialization = false;
        var model = null;

        $container = $(container);
        if ($container.data('map') == null) {
            isInitialization = true;

            var map = new SMap($container.find('.map')[0]);
            map.addDefaultLayer(SMap.DEF_BASE).enable();
            map.addDefaultControls();

            var layer = new SMap.Layer.Marker();
            map.addLayer(layer).enable();

            if (isResizable) {
                var sync = new SMap.Control.Sync();
                map.addControl(sync);
            }

            model = {
                map: map,
                layer: layer,
                interop: interop,
                isAdditive: false,
                isEmptyPoint: false,
                isAdding: false
            };
            $container.data('map', model);

            Map._BindEvents(model);
        }

        model = $container.data('map');
        var points = Map._SetMarkers(model, markers);

        model.isAdding = false;
        model.isEmptyPoint = points.length == 0 && !model.isAdditive;
        if (model.isEmptyPoint) {
            model.map.setCursor("crosshair");
            if (!isZoomed) {
                model.map.setZoom(1);
            }
        } else {
            model.map.setCursor("move");
            if (!isZoomed) {
                var centerZoom = model.map.computeCenterZoom(points);
                if (centerZoom[1] > 13) {
                    centerZoom[1] = 13;
                }

                model.map.setCenterZoom(centerZoom[0], centerZoom[1]);
            }
        }
    },
    _BindEvents: function (model) {
        function dragStart(e) {
            var node = e.target.getContainer();
            node[SMap.LAYER_MARKER].style.cursor = "grab";
        }

        function dragStop(e) {
            var node = e.target.getContainer();
            node[SMap.LAYER_MARKER].style.cursor = "";
            var coords = e.target.getCoords();

            var id = Number.parseInt(e.target.getId());
            moveMarkerOnCoords(id, coords);
        }

        function mapClick(e) {
            if (model.isEmptyPoint || model.isAdding) {
                var index = null;
                if (model.isEmptyPoint) {
                    index = 0;
                }

                var coords = SMap.Coords.fromEvent(e.data.event, model.map);
                moveMarkerOnCoords(index, coords);
            }
        }

        function markerClick(e) {
            var id = Number.parseInt(e.target.getId());
            model.interop.invokeMethodAsync("MarkerSelected", id);
        }

        function moveMarkerOnCoords(id, coords) {
            var latitude = coords.y;
            var longitude = coords.x;

            coords.getAltitude().then(function (altitude) {
                model.interop.invokeMethodAsync("MarkerMoved", id, latitude, longitude, altitude);
            });
        }

        var signals = model.map.getSignals();
        signals.addListener(window, "marker-drag-start", dragStart);
        signals.addListener(window, "marker-drag-stop", dragStop);
        signals.addListener(window, "map-click", mapClick);
        signals.addListener(window, "marker-click", markerClick);

        var $addButton = $container.find(".btn-add-location");

        $addButton.click(function () {
            model.isAdding = true;
            model.map.setCursor("crosshair");
        });

        model.isAdditive = $addButton.length > 0;
    },
    _SetMarkers: function (model, markers) {
        model.layer.removeAll();
        var points = [];
        for (var i = 0; i < markers.length; i++) {
            if (markers[i].longitude == null && markers[i].latitude == null) {
                continue;
            }

            var dropColor = markers[i].dropColor;
            if (dropColor == null) {
                dropColor = "red";
            }

            var options = {
                title: markers[i].title,
                url: SMap.CONFIG.img + "/marker/drop-" + dropColor + ".png"
            };
            var point = SMap.Coords.fromWGS84(markers[i].longitude, markers[i].latitude);
            var marker = new SMap.Marker(point, "" + i, options);

            if (markers[i].isEditable) {
                marker.decorate(SMap.Marker.Feature.Draggable);
            }

            model.layer.addMarker(marker);

            points.push(point);
        }

        return points;
    }
};