import React, { useState, useEffect } from 'react';

import { StateService } from './stateService';
import { Map, IMapMarker } from './Components/map';

export interface IAddressAndCoordinate {
    address: string;
    latitude: number;
    longitude: number;
}

const BusinessPage: React.FC = () => {
    const [mapMarkers, setMapMarkers] = useState<IMapMarker[]>([]);

    useEffect(() => {
        let jsonString = StateService.get<string>('businesspage.addressandcoordinatesjson');
        let header = StateService.get<string>('businesspage.header');
        let list: IAddressAndCoordinate[] = [];

        if (jsonString != null)
            list = JSON.parse(jsonString.toString());

        if (list != null && list.length > 0) {
            let markers: IMapMarker[] = [];
            list.forEach((a: IAddressAndCoordinate) => {
                markers.push({
                    latitude: a.latitude,
                    longitude: a.longitude,
                    popup: {address: a.address, description: '', pageLink: '', title: header ?? '' },
                    //title: `${header}<br/>${a.address}`
                });
            });
            setMapMarkers(markers);
        }
    }, []);

    return (
        <Map myPositionFitBound={true} markers={mapMarkers} />
    );
}

export default BusinessPage;