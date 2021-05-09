let isLoaded = false;

export function ensureApi(isPoi) {
    if (isLoaded) {
        return;
    }

    return new Promise(function (resolve, reject) {
        let options = {};

        if (isPoi) {
            options.poi = true;
        }

        window.Loader.lang = "en";
        window.Loader.async = true;
        window.Loader.load(null, options, function () {
            isLoaded = true;
            resolve();
        });
    });
}

export function initialize(container, interop, markers, isZoomed, isResizable, isEditable, isPoi) {
    let model = null;

    const $container = $(container);
    if ($container.data('map') == null) {
        const map = new SMap($container.find('.map')[0]);
        map.addDefaultLayer(SMap.DEF_BASE).enable();
        map.addDefaultControls();

        const layer = new SMap.Layer.Marker();
        map.addLayer(layer).enable();

        if (isResizable) {
            const sync = new SMap.Control.Sync();
            map.addControl(sync);
        }

        if (isPoi) {
            const poiLayer = new SMap.Layer.Marker(undefined, {
                poiTooltip: true
            });
            map.addLayer(poiLayer).enable();

            var dataProvider = map.createDefaultDataProvider();
            dataProvider.setOwner(map);
            dataProvider.addLayer(poiLayer);
            dataProvider.setMapSet(SMap.MAPSET_BASE);
            dataProvider.enable();
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
            bindEvents(model, $container);
        }
    }

    model = $container.data('map');
    var points = setMarkers(model, markers, isEditable);

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
            centerZoom[1] = ensureMaxCenterZoom(centerZoom[1]);
            model.map.setCenterZoom(centerZoom[0], centerZoom[1]);
        }
    }
}

function ensureMaxCenterZoom(zoom) {
    if (zoom > 13) {
        zoom = 13;
    }

    return zoom;
}

function bindEvents(model, $container) {
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
        if (!("getPOI" in e.target)) {
            var id = Number.parseInt(e.target.getId());
            model.interop.invokeMethodAsync("MapInterop.MarkerSelected", id);
        }
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
}

function setMarkers(model, markers, isEditable) {
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
}

export function search(container, query) {
    const model = $(container).data('map');
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
}

export function centerAt(container, latitude, longitude) {
    const model = $(container).data('map');
    model.map.setCenterZoom(SMap.Coords.fromWGS84(longitude, latitude), 15);
}