let isLoaded = false;
let Leaflet;
let countriesGeoJson = null;

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

async function ensureCountriesGeoJson() {
    if (countriesGeoJson != null) {
        return countriesGeoJson;
    }

    const response = await fetch("./_content/Recollections.Blazor.Components/countries.geo.json");
    countriesGeoJson = await response.json();
    return countriesGeoJson;
}

function pointInPolygon(lat, lng, polygon) {
    // Ray-casting algorithm for point-in-polygon
    // polygon is an array of [lng, lat] coordinate pairs
    let inside = false;
    for (let i = 0, j = polygon.length - 1; i < polygon.length; j = i++) {
        const xi = polygon[i][1], yi = polygon[i][0];
        const xj = polygon[j][1], yj = polygon[j][0];

        const intersect = ((yi > lng) !== (yj > lng))
            && (lat < (xj - xi) * (lng - yi) / (yj - yi) + xi);
        if (intersect) inside = !inside;
    }
    return inside;
}

function pointInGeometry(lat, lng, geometry) {
    if (geometry.type === "Polygon") {
        // Check the outer ring (index 0)
        return pointInPolygon(lat, lng, geometry.coordinates[0]);
    } else if (geometry.type === "MultiPolygon") {
        for (const polygon of geometry.coordinates) {
            if (pointInPolygon(lat, lng, polygon[0])) {
                return true;
            }
        }
    }
    return false;
}

function computeVisitedCountries(markers, geojson) {
    const visited = new Set();
    for (const marker of markers) {
        if (marker.latitude == null || marker.longitude == null) continue;
        for (let i = 0; i < geojson.features.length; i++) {
            const feature = geojson.features[i];
            if (visited.has(i)) continue;
            if (pointInGeometry(marker.latitude, marker.longitude, feature.geometry)) {
                visited.add(i);
                break;
            }
        }
    }
    return visited;
}

export function initialize(container, interop, isEditable) {
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
    const $container = $(container);
    const model = $container.data('map');
    
    model.lastMarkers = markers;
    model.lastIsEditable = isEditable;

    // In countries mode, update the countries layer instead of showing markers
    if (model.viewMode === "countries") {
        if (model.countriesLayer) {
            model.countriesLayer.remove();
            model.countriesLayer = null;
        }
        setViewMode(container, "countries", markers);
        return;
    }

    const points = setMarkers(model, markers, isEditable);

    model.isAdding = false;
    model.isEmptyPoint = points.length == 0 && !model.isAdditive;

    $container.find('.map').css("cursor", "");
    if (model.isEmptyPoint) {
        $container.find('.map').css("cursor", "crosshair");
    }
}

export function centerAtMarkers(container) {
    const $container = $(container);
    const model = $container.data('map');
    if (model.points.length == 0) {
        model.map.setView([0, 0], 1);
    } else {
        model.map.fitBounds(model.points, { maxZoom: 14 });
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

    model.points = points;
    return points;
}

function moveMarker(model, id, latitude, longitude) {
    model.interop.invokeMethodAsync("MapInterop.MarkerMoved", id, latitude, longitude);
}

export function centerAt(container, latitude, longitude, zoom) {
    const model = $(container).data('map');
    model.map.setView([latitude, longitude], zoom ?? 17);
}

export function redraw(container) {
    const model = $(container).data('map');
    model.tiles.redraw();
}

export async function setViewMode(container, mode, markers) {
    const model = $(container).data('map');
    if (!model) return;

    model.viewMode = mode;

    if (mode === "countries") {
        // Hide markers
        if (model.markers) {
            for (const m of model.markers) {
                m.remove();
            }
        }

        // Show countries layer
        if (!model.countriesLayer) {
            const geojson = await ensureCountriesGeoJson();
            const visited = computeVisitedCountries(markers || [], geojson);

            model.countriesLayer = Leaflet.geoJSON(geojson, {
                interactive: false,
                style: function (feature) {
                    const index = geojson.features.indexOf(feature);
                    const isVisited = visited.has(index);
                    return {
                        fillColor: isVisited ? "#FA8072" : "#dee2e6",
                        fillOpacity: isVisited ? 0.5 : 0.15,
                        color: isVisited ? "#E06050" : "#adb5bd",
                        weight: isVisited ? 2 : 0.5
                    };
                }
            });
        }
        model.countriesLayer.addTo(model.map);

        // Fit bounds to show the whole world
        if (model.points && model.points.length > 0) {
            model.map.fitBounds(model.points, { maxZoom: 5 });
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
