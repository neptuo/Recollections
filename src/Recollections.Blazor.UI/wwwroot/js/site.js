window.Bootstrap = {
    Modal: {
        Show: function (container) {
            var modal = bootstrap.Modal.getInstance(container);
            if (modal == null) {
                modal = new bootstrap.Modal(container, {
                    "show": true,
                    "focus": true
                });
            
                container.addEventListener('shown.bs.modal', function () {
                    $(container).find("input").first().trigger('focus');
                });
            }

            modal.show();
        },
        Hide: function (container) {
            var modal = bootstrap.Modal.getInstance(container);
            if (modal != null) {
                modal.hide();
            }
        },
        Dispose: function (container) {
            var modal = bootstrap.Modal.getInstance(container);
            if (modal != null) {
                modal.dispose();
            }
        }
    },
    Tooltip: {
        Init: function (container) {
            var tooltip = bootstrap.Tooltip.getInstance(container);
            if (tooltip == null) {
                tooltip = new bootstrap.Tooltip(container);
            }
        },
        Show: function (container) {
            var tooltip = bootstrap.Tooltip.getInstance(container);
            if (tooltip != null) {
                tooltip.show();
            }
        },
        Hide: function (container) {
            var tooltip = bootstrap.Tooltip.getInstance(container);
            if (tooltip != null) {
                tooltip.hide();
            }
        },
        Dispose: function (container) {
            var tooltip = bootstrap.Tooltip.getInstance(container);
            if (tooltip != null) {
                tooltip.dispose();
            }
        }
    },
    Dropdown: {
        Init: function (container) {
            var dropDown = bootstrap.Dropdown.getInstance(container);
            if (dropDown == null) {
                dropDown = new bootstrap.Dropdown(container);
            }
        },
        Show: function (container) {
            var dropDown = bootstrap.Dropdown.getInstance(container);
            if (dropDown != null) {
                dropDown.show();
            }
        },
        Hide: function (container) {
            var dropDown = bootstrap.Dropdown.getInstance(container);
            if (dropDown != null) {
                dropDown.hide();
            }
        },
        Dispose: function (container) {
            var dropDown = bootstrap.Dropdown.getInstance(container);
            if (dropDown != null) {
                dropDown.dispose();
            }
        }
    },
    Popover: {
        Show: function (container) {
            var popover = bootstrap.Popover.getInstance(container);
            if (popover == null) {
                popover = new bootstrap.Popover(container, {
                    placement: "bottom"
                });
            }
        },
        Dispose: function (container) {
            var popover = bootstrap.Popover.getInstance(container);
            if (popover != null) {
                popover.dispose();
            }
        }
    }
};

window.ElementReference = {
    ScrollIntoView: function (element) {
        element.scrollIntoView();
    },
    Blur: function (element) {
        element.blur();
    }
};

window.Recollections = {
    NavigateTo: function (href) {
        window.location.href = href;
        return true;
    },
    SetTitle: function (title) {
        document.title = title;
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
    Initialize: function (interop, textArea, value) {
        $textArea = $(textArea);
        if ($textArea.data("easymde") != null) {
            return;
        }

        var editor = new EasyMDE({
            autoDownloadFontAwesome: false,
            element: textArea,
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
                    className: "fa fa-times float-end",
                    title: "Close Editor",
                    action: function (editor) {
                        interop.invokeMethodAsync("Markdown.OnCancel");
                    }
                },
                {
                    name: "save",
                    className: "fa fa-check float-end",
                    title: "Save",
                    action: function (editor) {
                        var value = editor.value();
                        interop.invokeMethodAsync("Markdown.OnSave", value);
                    }
                }
            ],
            shortcuts: {
                "save": "Ctrl-Enter",
                "cancel": "Escape"
            }
        });

        $textArea.data("easymde", editor);

        if (value !== null) {
            InlineMarkdownEdit.SetValue(textArea, value);
        }
    },
    Destroy: function (textArea) {
        var editor = $(textArea).data("easymde");
        if (editor != null) {
            editor.toTextArea();
        }
    },
    SetValue: function (textArea, value) {
        if (value === null) {
            value = "";
        }

        var editor = $(textArea).data("easymde");
        if (editor != null) {
            editor.value(value);
        }
    },
    GetValue: function (textArea) {
        var editor = $(textArea).data("easymde");
        if (editor != null) {
            return editor.value();
        }
    }
};

window.InlineTextEdit = {
    Initialize: function (interop, input) {
        $(input).focus().keyup(function (e) {
            if (e.keyCode == 27) {
                $(this).blur();
                setTimeout(function () {
                    interop.invokeMethodAsync("TextEdit.OnCancel");
                }, 1);
            }
        });
    }
};

window.InlineDateEdit = {
    Initialize: function (input, format) {
        $(input).focus().datepicker({
            format: format.toLowerCase(),
            autoclose: true,
            todayHighlight: true,
            todayBtn: "linked"
        });
    },
    Destroy: function (input) {
        $(input).datepicker("destroy");
    },
    GetValue: function (input) {
        return $(input).val();
    }
};

window.DatePicker = {
    Initialize: function (input, format) {
        $(input).datepicker({
            format: format.toLowerCase(),
            autoclose: true,
            todayHighlight: true,
            todayBtn: "linked"
        });
    },
    Destroy: function (input) {
        if (input != null) {
            $(input).datepicker("destroy");
        }
    },
    GetValue: function (input) {
        return $(input).val();
    }
}

window.Downloader = {
    FromUrlAsync: function (name, url) {
        var link = document.createElement("a");
        link.target = "_blank";
        link.download = name;
        link.href = url;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }
};

window.MapInterop = {
    Initialize: function (container, interop, markers, isZoomed, isResizable, isEditable) {
        var model = null;

        $container = $(container);
        if ($container.data('map') == null) {
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

            if (isEditable) {
                MapInterop._BindEvents(model);
            }
        }

        model = $container.data('map');
        var points = MapInterop._SetMarkers(model, markers, isEditable);

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
                centerZoom[1] = MapInterop._EnsureMaxCenterZoom(centerZoom[1]);
                model.map.setCenterZoom(centerZoom[0], centerZoom[1]);
            }
        }
    },
    _EnsureMaxCenterZoom(zoom) {
        if (zoom > 13) {
            zoom = 13;
        }

        return zoom;
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
            model.interop.invokeMethodAsync("MapInterop.MarkerSelected", id);
        }

        function moveMarkerOnCoords(id, coords) {
            var latitude = coords.y;
            var longitude = coords.x;

            coords.getAltitude().then(function (altitude) {
                model.interop.invokeMethodAsync("MapInterop.MarkerMoved", id, latitude, longitude, altitude);
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
    _SetMarkers: function (model, markers, isEditable) {
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

            if (isEditable && markers[i].isEditable) {
                marker.decorate(SMap.Marker.Feature.Draggable);
            }

            model.layer.addMarker(marker);
            points.push(point);
        }

        return points;
    },
    Search: function (container, query) {
        model = $(container).data('map');
        new SMap.Geocoder(query, function (geocoder) {
            var data = [];
            var results = geocoder.getResults()[0].results;
            for (var i = 0; i < results.length; i++) {
                var result = results[i];
                data.push({
                    label: result.label,
                    longitude: result.coords.x,
                    latitude: result.coords.y
                });
            }

            model.interop.invokeMethodAsync("MapInterop.SearchCompleted", data);
        });
    },
    CenterAt: function (container, latitude, longitude) {
        model = $(container).data('map');
        model.map.setCenterZoom(SMap.Coords.fromWGS84(longitude, latitude), 15);
    }
};