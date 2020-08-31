import React, { useState, useEffect } from 'react';
import { StateService } from './stateService';
import { Map } from './Components/map';
var BusinessPage = function () {
    var _a = useState([]), mapMarkers = _a[0], setMapMarkers = _a[1];
    useEffect(function () {
        var jsonString = StateService.get('businesspage.addressandcoordinatesjson');
        var header = StateService.get('businesspage.header');
        var list = [];
        if (jsonString != null)
            list = JSON.parse(jsonString.toString());
        if (list != null && list.length > 0) {
            var markers_1 = [];
            list.forEach(function (a) {
                markers_1.push({
                    latitude: a.latitude,
                    longitude: a.longitude,
                    popup: { address: a.address, description: '', pageLink: '', title: header !== null && header !== void 0 ? header : '' },
                });
            });
            setMapMarkers(markers_1);
        }
    }, []);
    return (React.createElement(Map, { myPositionFitBound: true, markers: mapMarkers }));
};
export default BusinessPage;
//# sourceMappingURL=businessPage.js.map