import React, { FunctionComponent, useState, useEffect, useRef } from 'react';
import * as L from "leaflet";
import { GestureHandling } from "leaflet-gesture-handling";
import 'leaflet';
import "leaflet.markercluster";
import 'leaflet-gesture-handling';
import 'leaflet.fullscreen';
import { getText } from '../Helpers/getText';
import { getService } from '../bussinessService';

export interface IMapMarker {
    latitude: number;
    longitude: number;
    popup: IMapMarkerPopup;
}

export interface IMapMarkerPopup {
    pageLink: string;
    title: string;
    description: string;
    address: string;
}

export interface IMapRequest {
    myPositionFitBound: boolean;
    markers: IMapMarker[];
    viewLatLng?: [number, number];
    positionSet?: (latlng: [number, number]) => void;
}

export interface ITextTranslation {
    iconAltText: string;
    yourPositionText: string;
    geoLocationFailedText: string;
    loadingText: string;
    visitButtonText: string;
}

// url: https://stackoverflow.com/a/56057723
export const Map: FunctionComponent<IMapRequest> = (props: IMapRequest) => {
    const mapMaxZoom: number = 18;
    const mapMinZoom: number = 5;
    const mapZoomInLevel: number = 15;
    const mapDisableClusteringAtZoomLevel: number = 14;

    var myPositionMarker: L.Marker;

    const [loadingComplete, setLoadingComplete] = useState(false);
    const [textTranslations, setTextTranslations] = useState<ITextTranslation>();

    useEffect(() => {
        let _service = getService();
        if (_service != null) {
            _service.getTranslations().then(translations => {
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

    useEffect(() => {
        let latlng = props.viewLatLng;
        if (latlng != null && latlng.length > 0 && mapRef.current != null) {
            let latlngLiteral: L.LatLngLiteral = { lat: latlng[0], lng: latlng[1] };
            mapRef.current.setView(latlngLiteral, mapZoomInLevel);
        }
    }, [props.viewLatLng]);

    // Create Map
    const mapRef = useRef<L.Map | null>(null);
    useEffect(() => {
        L.Map.addInitHook("addHandler", "gestureHandling", GestureHandling);

        mapRef.current = (L as any).map('map', { // (L as any)
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
    const layerRef = useRef<L.LayerGroup | null>(null);
    useEffect(() => {
        if (mapRef.current !== null) {
            layerRef.current = L.markerClusterGroup({
                showCoverageOnHover: false, // When you mouse over a cluster it shows the bounds of its markers.
                disableClusteringAtZoom: mapDisableClusteringAtZoomLevel,
            });
        }
    }, []);

    // Update Markers
    useEffect(() => {
        var featureGroup = new L.FeatureGroup();
        let markers: L.Marker[] = [];

        if (layerRef.current != null) {
            layerRef.current.clearLayers();
        }

        props.markers.forEach(marker => {
            if (layerRef.current !== null) {
                const latlng: L.LatLngExpression = [marker.latitude, marker.longitude];
                let p = marker.popup;
                let customPopup = getMapPopupCard(p.pageLink, p.title, p.description, p.address);

                // specify popup options 
                let customOptions = {
                    // 'maxWidth': '500',
                    'className': '' // text-capitalize
                }

                let lMarker = L.marker(latlng, {
                    icon: L.icon({
                        iconUrl: '/media/leaflet/marker.svg',
                        iconSize: [35, 35], // size of the icon
                        iconAnchor: [17, 35], // point of the icon which will correspond to marker's location
                        popupAnchor: [0, -35], // point from which the popup should open relative to the iconAnchor
                    }),
                    alt: textTranslations?.iconAltText
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

    const getMapPopupCard = (pageLink: string, title: string, description: string, address: string): string => {
        // card, card-body
        let link = '';
        if (pageLink.length > 0) {
            link = `<a href="${pageLink}" class="btn btn-primary w-100" role="button">${textTranslations?.visitButtonText}</a>`;
        }
        return `<div>
          <h5 class="card-title">${title}</h5>
          <h6 class="card-subtitle mb-0 text-muted">${address}</h6>
          <p class="card-text my-3">${description}</p>
          ${link}
        </div>`;
    };

    const getLocationAndZoomInToInMap = () => {
        if (navigator.geolocation) {
            let options: PositionOptions = {
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
        } else {
            // TODO: Hide position button!?!?
            console.log('navigator.geolocation is null.');
            //const positionIcon = document.getElementById(this.myPositionElementId) as HTMLElement;
            //positionIcon.classList.add('d-none');
        }
    }

    const currentPosition = (position: Position) => {
        removeMyPositionMarker();
        let latlng: L.LatLngLiteral = { lat: position.coords.latitude, lng: position.coords.longitude };

        if (props.positionSet != null)
            props.positionSet([position.coords.latitude, position.coords.longitude]);

        let icon = L.icon({
            iconUrl: '/media/leaflet/marker-home.svg',
            iconSize: [35, 35],
            iconAnchor: [17, 35],
            popupAnchor: [0, -35],
        });

        let popUpText = textTranslations?.yourPositionText == null ? "" : textTranslations.yourPositionText;

        myPositionMarker = L.marker(latlng, {
            icon: icon,
            alt: textTranslations?.iconAltText
        }).bindPopup(popUpText);

        if (mapRef.current != null && layerRef.current != null) {
            layerRef.current.addLayer(myPositionMarker);

            if (props.myPositionFitBound) {
                const fitBounds: L.LatLngBoundsExpression = [];
                fitBounds.push([latlng.lat, latlng.lng]);
                props.markers.forEach(m => {
                    fitBounds.push([m.latitude, m.longitude]);
                });
                mapRef.current.fitBounds(fitBounds, { duration: 1.2, padding: [15, 15] });
            }
            else {
                mapRef.current.setView(latlng, mapZoomInLevel);
            }
        }
    }

    const currentPositionErrorCallback = (error: PositionError) => {
        console.log(textTranslations?.geoLocationFailedText, error);
    }

    const removeMyPositionMarker = () => {
        if (myPositionMarker != null && mapRef.current != null) {
            mapRef.current.removeLayer(myPositionMarker);
        }
    }

    //var foundString = (!props.myPositionFitBound && loadingComplete);

    return (
        <div className="map-container position-relative pb-3">
            {/* {foundString ? (
                <div>Visar {props.markers.length} kartpositioner</div>
            ) : (
                <div>&nbsp;</div>
            )} */}
            <div id="map" className="map-element w-100">
                {!loadingComplete &&
                    <div className="text-center w-100 mt-5 h4">{textTranslations?.loadingText}</div>
                }
            </div>
            <div className="my-position-button">
                <button className="btn btn-primary" title={textTranslations?.yourPositionText} onClick={getLocationAndZoomInToInMap}>
                    <i className="fas fa-crosshairs"></i>
                </button>
            </div>
        </div>
    );
}