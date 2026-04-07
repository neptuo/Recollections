let isLoaded = false;
let Leaflet;

let countriesStyleInjected = false;
const _mapData = new WeakMap();

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

export function initialize(container, interop, isEditable) {
    let model = null;

    if (!_mapData.has(container)) {
        const map = Leaflet.map(container.querySelector('.map'));
        map.zoomControl.setPosition("topright");

        model = {
            map: map,
            tiles: null,
            interop: interop,
            isAdditive: false,
            isEmptyPoint: false,
            isAdding: false
        };
        _mapData.set(container, model);

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
            bindEvents(model, container);
        }

        model.map.on("moveend", () => {
            const center = model.map.getCenter(); // { lat, lng }
            const zoom = model.map.getZoom();
            model.interop.invokeMethod("MapInterop.MoveEnd", center.lat, center.lng, zoom);
        });
    }

    // model = $container.data('map');
    // const points = setMarkers(model, markers, isEditable);

    // model.isAdding = false;
    // model.isEmptyPoint = points.length == 0 && !model.isAdditive;

    // $container.find('.map').css("cursor", "");
    // if (model.isEmptyPoint || points.length == 0) {
    //     $container.find('.map').css("cursor", "crosshair");
    //     if (!isZoomed) {
    //         model.map.setView([0, 0], 1);
    //     }
    // } else {
    //     if (!isZoomed) {
    //         model.map.fitBounds(points, { maxZoom: 14 });
    //     }
    // }
}

export function updateMarkers(container, markers, isEditable) {
    const model = _mapData.get(container);
    
    model.lastMarkers = markers;
    model.lastIsEditable = isEditable;

    // In countries mode, don't show markers
    if (model.viewMode === "countries") {
        return;
    }

    const points = setMarkers(model, markers, isEditable);

    model.isAdding = false;
    model.isEmptyPoint = points.length == 0 && !model.isAdditive;

    const mapEl = container.querySelector('.map');
    mapEl.style.cursor = "";
    if (model.isEmptyPoint) {
        mapEl.style.cursor = "crosshair";
    }
}

export function centerAtMarkers(container) {
    const model = _mapData.get(container);
    if (model.points.length == 0) {
        model.map.setView([0, 0], 1);
    } else {
        model.map.fitBounds(model.points, { maxZoom: 14 });
    }
}

function bindEvents(model, container) {
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

    var addButton = container.querySelector(".btn-add-location");

    if (addButton) {
        addButton.addEventListener('click', function () {
            model.isAdding = true;
            container.querySelector('.map').style.cursor = "crosshair";
        });
    }

    model.isAdditive = addButton != null;
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

    model.points = points;
    return points;
}

function moveMarker(model, id, latitude, longitude) {
    model.interop.invokeMethodAsync("MapInterop.MarkerMoved", id, latitude, longitude);
}

export function centerAt(container, latitude, longitude, zoom) {
    const model = _mapData.get(container);
    model.map.setView([latitude, longitude], zoom ?? 17);
}

export function redraw(container) {
    const model = _mapData.get(container);
    model.tiles.redraw();
}

export function setViewMode(container, mode, countriesGeoJsonString) {
    const model = _mapData.get(container);
    if (!model) return;

    model.viewMode = mode;

    if (mode === "countries" && countriesGeoJsonString) {
        // Hide markers
        if (model.markers) {
            for (const m of model.markers) {
                m.remove();
            }
        }

        // Remove previous countries layer
        if (model.countriesLayer) {
            model.countriesLayer.remove();
            model.countriesLayer = null;
        }

        // Parse and render server-provided visited countries GeoJSON
        const geojson = JSON.parse(countriesGeoJsonString);

        model.countriesLayer = Leaflet.geoJSON(geojson, {
            style: function () {
                return {
                    fillColor: "#FA8072",
                    fillOpacity: 0.5,
                    color: "#E06050",
                    weight: 2
                };
            },
            onEachFeature: function (feature, layer) {
                if (feature.properties && feature.properties.name) {
                    layer.bindTooltip(feature.properties.name);
                }
            }
        });

        if (!countriesStyleInjected) {
            countriesStyleInjected = true;
            const style = document.createElement("style");
            style.textContent = ".leaflet-overlay-pane path { outline: none !important; }";
            document.head.appendChild(style);
        }

        model.countriesLayer.addTo(model.map);

        // Fit to visited countries bounds, or show world
        const bounds = model.countriesLayer.getBounds();
        if (bounds.isValid()) {
            model.map.fitBounds(bounds, { maxZoom: 5, padding: [20, 20] });
        } else {
            model.map.setView([20, 0], 2);
        }
    } else {
        // Remove countries layer
        if (model.countriesLayer) {
            model.countriesLayer.remove();
            model.countriesLayer = null;
        }

        // Re-add markers
        if (model.lastMarkers && model.lastIsEditable !== undefined) {
            setMarkers(model, model.lastMarkers, model.lastIsEditable);
        }

        if (model.points && model.points.length > 0) {
            model.map.fitBounds(model.points, { maxZoom: 14 });
        } else {
            model.map.setView([0, 0], 1);
        }
    }
}
