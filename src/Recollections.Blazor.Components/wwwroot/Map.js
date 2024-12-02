let isLoaded = false;
let Leaflet;

export async function ensureApi() {
    if (isLoaded) {
        return;
    }

    isLoaded = true;

    const style = document.createElement("link");
    style.rel = "stylesheet";
    style.href = "./_content/Recollections.Blazor.Components/leaflet/leaflet.css";
    document.head.appendChild(style);

    await import('./leaflet/leaflet-src.js');
    Leaflet = window.L;
}

export function initialize(container, interop, markers, isZoomed, isEditable) {
    let model = null;

    const $container = $(container);
    if ($container.data('map') == null) {
        const map = Leaflet.map($container.find('.map')[0]);
        map.zoomControl.setPosition("topright");

        model = {
            map: map,
            tiles: null,
            interop: interop,
            isAdditive: false,
            isEmptyPoint: false,
            isAdding: false
        };
        $container.data('map', model);

        // Map layer
        const BackendLayer = L.TileLayer.extend({
            minZoom: 0,
            maxZoom: 19,
            attribution: '<a href="https://api.mapy.cz/copyright" target="_blank">&copy; Seznam.cz a.s. a další</a>',
            createTile: function (coords, done) {
                const img = document.createElement('img');
                img.setAttribute('role', 'presentation');

                async function loadTile() {
                    await model.interop.invokeMethodAsync("MapInterop.LoadTile", DotNet.createJSObjectReference(img), coords.x, coords.y, coords.z);
                    done(null, img);
                };
                loadTile();

                return img;
            }
        });
        const tiles = new BackendLayer();
        tiles.addTo(map);
        model.tiles = tiles;

        // Attribution to mapy.cz
        const LogoControl = Leaflet.Control.extend({
            options: {
                position: 'bottomleft',
            },

            onAdd: () => {
                const container = Leaflet.DomUtil.create('div');
                const link = Leaflet.DomUtil.create('a', '', container);

                link.setAttribute('href', 'http://mapy.cz/');
                link.setAttribute('target', '_blank');
                link.innerHTML = '<img src="https://api.mapy.cz/img/api/logo.svg" />';
                Leaflet.DomEvent.disableClickPropagation(link);

                return container;
            },
        });
        new LogoControl().addTo(map);

        if (isEditable) {
            bindEvents(model, $container);
        }
    }

    model = $container.data('map');
    const points = setMarkers(model, markers, isEditable);

    model.isAdding = false;
    model.isEmptyPoint = points.length == 0 && !model.isAdditive;

    $container.find('.map').css("cursor", "");
    if (model.isEmptyPoint || points.length == 0) {
        $container.find('.map').css("cursor", "crosshair");
        if (!isZoomed) {
            model.map.setView([0, 0], 1);
        }
    } else {
        if (!isZoomed) {
            model.map.fitBounds(points, { maxZoom: 14 });
        }
    }
}

function bindEvents(model, $container) {
    function mapClick(e) {
        if (model.isEmptyPoint || model.isAdding) {
            var id = null;
            if (model.isEmptyPoint) {
                id = 0;
            }

            model.isAdding = false;
            moveMarker(model, id, e.latlng.lat, e.latlng.lng);
        }
    }

    model.map.on("click", mapClick);

    var $addButton = $container.find(".btn-add-location");

    $addButton.click(function () {
        model.isAdding = true;
        $container.find('.map').css("cursor", "crosshair");
    });

    model.isAdditive = $addButton.length > 0;
}

function setMarkers(model, markers, isEditable) {
    if (model.markers) {
        for (var i = 0; i < model.markers.length; i++) {
            model.markers[i].remove();
        }
    }

    model.markers = [];
    const points = [];
    for (var i = 0; i < markers.length; i++) {
        if (markers[i].longitude == null && markers[i].latitude == null) {
            continue;
        }

        var dropColor = markers[i].dropColor;
        if (dropColor == null) {
            dropColor = "red";
        }

        var icon = Leaflet.icon({
            iconUrl: "https://api.mapy.cz/img/api/marker/drop-" + dropColor + ".png",

            iconSize: [22, 31], // size of the icon
            iconAnchor: [11, 31], // point of the icon which will correspond to marker's location
            popupAnchor: [11, 0] // point from which the popup should open relative to the iconAnchor
        });

        const point = [markers[i].latitude, markers[i].longitude];
        points.push(point);

        const markerOptions = {
            icon: icon,
            title: markers[i].title,
            draggable: isEditable && markers[i].isEditable
        };

        const marker = Leaflet.marker(point, markerOptions).addTo(model.map);
        marker.id = i;
        if (isEditable) {
            marker.on("click", e => {
                model.interop.invokeMethodAsync("MapInterop.MarkerSelected", e.target.id);
            });
            marker.on("dragend", e => {
                const latitude = e.target.getLatLng().lat;
                const longitude = e.target.getLatLng().lng;
                moveMarker(model, e.target.id, latitude, longitude);
            });
        }
        model.markers.push(marker);
    }

    return points;
}

function moveMarker(model, id, latitude, longitude) {
    model.interop.invokeMethodAsync("MapInterop.MarkerMoved", id, latitude, longitude);
}

export function centerAt(container, latitude, longitude) {
    const model = $(container).data('map');
    model.map.setView([latitude, longitude], 17);
}

export function redraw(container) {
    const model = $(container).data('map');
    model.tiles.redraw();
}