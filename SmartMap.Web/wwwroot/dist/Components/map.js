import React, { useState, useEffect, useRef } from 'react';
import * as L from "leaflet";
import { GestureHandling } from "leaflet-gesture-handling";
import 'leaflet';
import "leaflet.markercluster";
import 'leaflet-gesture-handling';
import 'leaflet.fullscreen';
import { getText } from '../Helpers/getText';
import { getService } from '../bussinessService';
// url: https://stackoverflow.com/a/56057723
export var Map = function (props) {
    var mapMaxZoom = 18;
    var mapMinZoom = 5;
    var mapZoomInLevel = 15;
    var mapDisableClusteringAtZoomLevel = 14;
    var myPositionMarker;
    var _a = useState(false), loadingComplete = _a[0], setLoadingComplete = _a[1];
    var _b = useState(), textTranslations = _b[0], setTextTranslations = _b[1];
    useEffect(function () {
        var _service = getService();
        if (_service != null) {
            _service.getTranslations().then(function (translations) {
                setTextTranslations({
                    iconAltText: getText('map.icon-alt', translations, 'Icon made by Freepik from www.flaticon.com'),
                    yourPositionText: getText('map.your-position', translations, 'Din position'),
                    geoLocationFailedText: getText('map.geo-location-failed', translations, 'Gick inte att hämta din position'),
                    loadingText: getText('common.loading', translations, 'Laddar...'),
                    visitButtonText: getText('map.popup-visit-button', translations, 'Besök'),
                });
            });
        }
    }, []);
    useEffect(function () {
        var latlng = props.viewLatLng;
        if (latlng != null && latlng.length > 0 && mapRef.current != null) {
            var latlngLiteral = { lat: latlng[0], lng: latlng[1] };
            mapRef.current.setView(latlngLiteral, mapZoomInLevel);
        }
    }, [props.viewLatLng]);
    // Create Map
    var mapRef = useRef(null);
    useEffect(function () {
        L.Map.addInitHook("addHandler", "gestureHandling", GestureHandling);
        mapRef.current = L.map('map', {
            zoomControl: true,
            gestureHandling: true,
            keyboard: true,
            fullscreenControl: true,
            fullscreenControlOptions: {
                position: 'topleft'
            }
        });
        if (mapRef.current != null) {
            //mapRef.current.zoomControl.setPosition('topright');
            L.tileLayer('https://{s}.tile.osm.org/{z}/{x}/{y}.png', {
                attribution: '&copy; <a href="https://osm.org/copyright">OpenStreetMap</a> contributors',
                maxZoom: mapMaxZoom,
                minZoom: mapMinZoom
            }).addTo(mapRef.current);
        }
    }, []);
    // Add Layer
    var layerRef = useRef(null);
    useEffect(function () {
        if (mapRef.current !== null) {
            layerRef.current = L.markerClusterGroup({
                showCoverageOnHover: false,
                disableClusteringAtZoom: mapDisableClusteringAtZoomLevel,
            });
        }
    }, []);
    // Update Markers
    useEffect(function () {
        var featureGroup = new L.FeatureGroup();
        var markers = [];
        if (layerRef.current != null) {
            layerRef.current.clearLayers();
        }
        props.markers.forEach(function (marker) {
            if (layerRef.current !== null) {
                var latlng = [marker.latitude, marker.longitude];
                var p = marker.popup;
                var customPopup = getMapPopupCard(p.pageLink, p.title, p.description, p.address);
                // specify popup options 
                var customOptions = {
                    // 'maxWidth': '500',
                    'className': '' // text-capitalize
                };
                var lMarker = L.marker(latlng, {
                    icon: L.icon({
                        iconUrl: '/media/leaflet/marker.svg',
                        iconSize: [35, 35],
                        iconAnchor: [17, 35],
                        popupAnchor: [0, -35],
                    }),
                    alt: textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.iconAltText
                }).bindPopup(customPopup, customOptions);
                markers.push(lMarker);
                layerRef.current.addLayer(lMarker);
                featureGroup.addLayer(lMarker);
            }
        });
        if (mapRef.current != null && layerRef.current != null)
            mapRef.current.addLayer(layerRef.current);
        if (mapRef.current !== null && featureGroup.getBounds().isValid()) {
            mapRef.current.fitBounds(featureGroup.getBounds(), { padding: [30, 30], maxZoom: 16 });
            setLoadingComplete(true);
        }
    }, [props.markers]);
    var getMapPopupCard = function (pageLink, title, description, address) {
        // card, card-body
        var link = '';
        if (pageLink.length > 0) {
            link = "<a href=\"" + pageLink + "\" class=\"btn btn-primary w-100\" role=\"button\">" + (textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.visitButtonText) + "</a>";
        }
        return "<div>\n          <h5 class=\"card-title\">" + title + "</h5>\n          <h6 class=\"card-subtitle mb-0 text-muted\">" + address + "</h6>\n          <p class=\"card-text my-3\">" + description + "</p>\n          " + link + "\n        </div>";
    };
    var getLocationAndZoomInToInMap = function () {
        if (navigator.geolocation) {
            var options = {
                enableHighAccuracy: true,
                timeout: 5000,
                maximumAge: 0
            };
            // if (browser.name === 'ie') {
            //     options = {
            //         enableHighAccuracy: false,
            //         maximumAge: 50000
            //     };
            // }
            navigator.geolocation.getCurrentPosition(currentPosition, currentPositionErrorCallback, options);
        }
        else {
            // TODO: Hide position button!?!?
            console.log('navigator.geolocation is null.');
            //const positionIcon = document.getElementById(this.myPositionElementId) as HTMLElement;
            //positionIcon.classList.add('d-none');
        }
    };
    var currentPosition = function (position) {
        removeMyPositionMarker();
        var latlng = { lat: position.coords.latitude, lng: position.coords.longitude };
        if (props.positionSet != null)
            props.positionSet([position.coords.latitude, position.coords.longitude]);
        var icon = L.icon({
            iconUrl: '/media/leaflet/marker-home.svg',
            iconSize: [35, 35],
            iconAnchor: [17, 35],
            popupAnchor: [0, -35],
        });
        var popUpText = (textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.yourPositionText) == null ? "" : textTranslations.yourPositionText;
        myPositionMarker = L.marker(latlng, {
            icon: icon,
            alt: textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.iconAltText
        }).bindPopup(popUpText);
        if (mapRef.current != null && layerRef.current != null) {
            layerRef.current.addLayer(myPositionMarker);
            if (props.myPositionFitBound) {
                var fitBounds_1 = [];
                fitBounds_1.push([latlng.lat, latlng.lng]);
                props.markers.forEach(function (m) {
                    fitBounds_1.push([m.latitude, m.longitude]);
                });
                mapRef.current.fitBounds(fitBounds_1, { duration: 1.2, padding: [15, 15] });
            }
            else {
                mapRef.current.setView(latlng, mapZoomInLevel);
            }
        }
    };
    var currentPositionErrorCallback = function (error) {
        console.log(textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.geoLocationFailedText, error);
    };
    var removeMyPositionMarker = function () {
        if (myPositionMarker != null && mapRef.current != null) {
            mapRef.current.removeLayer(myPositionMarker);
        }
    };
    //var foundString = (!props.myPositionFitBound && loadingComplete);
    return (React.createElement("div", { className: "map-container position-relative pb-3" },
        React.createElement("div", { id: "map", className: "map-element w-100" }, !loadingComplete &&
            React.createElement("div", { className: "text-center w-100 mt-5 h4" }, textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.loadingText)),
        React.createElement("div", { className: "my-position-button" },
            React.createElement("button", { className: "btn btn-primary", title: textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.yourPositionText, onClick: getLocationAndZoomInToInMap },
                React.createElement("i", { className: "fas fa-crosshairs" })))));
};
//# sourceMappingURL=map.js.map