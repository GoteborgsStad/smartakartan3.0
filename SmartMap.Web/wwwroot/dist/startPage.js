import React, { useState, useEffect } from 'react';
import { StateService } from './stateService';
import { DefaultICardResponse } from './service-model';
import { BussinessService } from './bussinessService';
import { Cards, DefaultSortValue } from './Components/cards';
import { Map } from './Components/map';
import { SearchBar, ISearchStateDefaultValue } from './Components/searchBar';
var StartPage = function () {
    var _a = useState([]), mapMarkers = _a[0], setMapMarkers = _a[1];
    var _b = useState(0), currentPage = _b[0], setCurrentPage = _b[1];
    var _c = useState(ISearchStateDefaultValue), searchState = _c[0], setSearchState = _c[1];
    var _d = useState(DefaultICardResponse), cardResponse = _d[0], setCardResponse = _d[1];
    var _e = useState([]), transactionTags = _e[0], setTransactionTags = _e[1];
    var _f = useState(DefaultSortValue), sortValue = _f[0], setSortValue = _f[1];
    var _g = useState([]), filterTags = _g[0], setFilterTags = _g[1];
    // const [filterMainTags, setFilterMainTags] = useState<ITag[]>([]);
    // const [filterSubTags, setFilterSubTags] = useState<ITag[]>([]);
    var _h = useState(0), randomSeed = _h[0], setRandomSeed = _h[1];
    useEffect(function () {
        queryTransactionTags();
    }, []);
    useEffect(function () {
        queryCards(sortValue, 0, getTicks());
        queryCardCoodrinates();
    }, [searchState]);
    // url: https://stackoverflow.com/a/7966778
    var getTicks = function () {
        return (621355968e9 + (new Date()).getTime() * 1e4);
    };
    var getService = function () {
        var baseUrl = StateService.get('common.baseurl');
        var region = StateService.get('common.region');
        var languageCode = StateService.get('common.languagecode');
        if (region === null)
            region = '';
        if (languageCode === null)
            languageCode = '';
        var service = null;
        if (baseUrl != null) {
            service = new BussinessService(baseUrl);
        }
        return {
            languageCode: languageCode,
            region: region,
            service: service
        };
    };
    var searchCallback = function (state) {
        setCurrentPage(0);
        setSearchState(state);
    };
    var paginationCallback = function (page) {
        setCurrentPage(page);
        queryCards(sortValue, page, randomSeed);
    };
    var sortingChange = function (sorting) {
        setSortValue(sorting);
        setCurrentPage(0);
        queryCards(sorting, 0, getTicks());
    };
    var queryTransactionTags = function () {
        var serviceReponse = getService();
        if (serviceReponse.service != null) {
            serviceReponse.service.getTransactionTags(serviceReponse.languageCode)
                .then(function (response) {
                if (response != null) {
                    response.forEach(function (t) { return t.checked = false; });
                    setTransactionTags(response);
                }
            });
        }
    };
    var queryCards = function (sorting, page, randomSeed) {
        var serviceReponse = getService();
        if (serviceReponse.service != null) {
            if (sorting == "Random")
                setRandomSeed(randomSeed);
            var tagString = commaSeparetedString(searchState.tags);
            var transactionTagString = commaSeparetedString(searchState.transactionTags);
            serviceReponse.service.getCards(searchState.query, tagString, transactionTagString, serviceReponse.region, serviceReponse.languageCode, page, searchState.digital, searchState.openNow, sorting, randomSeed)
                .then(function (response) {
                if (response.data != null) {
                    setCardResponse({
                        items: page === 0 ? response.data.items : cardResponse.items.concat(response.data.items),
                        itemsPerPage: response.data.itemsPerPage,
                        total: response.data.total,
                        //mainTags: response.data.mainTags,
                        //subTags: response.data.subTags
                        filterTags: response.data.filterTags
                    });
                    if (page == 0) {
                        setFilterTags(response.data.filterTags);
                    }
                }
            });
        }
    };
    var commaSeparetedString = function (arr) {
        return arr.join(",");
    };
    var queryCardCoodrinates = function () {
        var serviceReponse = getService();
        if (serviceReponse.service != null) {
            var tagString = commaSeparetedString(searchState.tags);
            var transactionTagString = commaSeparetedString(searchState.transactionTags);
            serviceReponse.service.getCardCoordinates(searchState.query, tagString, transactionTagString, serviceReponse.region, serviceReponse.languageCode, searchState.digital, searchState.openNow)
                .then(function (response) {
                if (response != null) {
                    fillMapMarkers(response);
                }
            });
        }
    };
    var fillMapMarkers = function (cards) {
        var markers = [];
        cards.forEach(function (c) {
            if (c.addressAndCoordinates != null && c.addressAndCoordinates.length > 0) {
                c.addressAndCoordinates.forEach(function (a) {
                    markers.push({
                        latitude: a.latitude,
                        longitude: a.longitude,
                        popup: { address: a.address, description: c.description, pageLink: c.detailPageLink, title: c.header },
                    });
                });
            }
        });
        setMapMarkers(markers);
    };
    var _j = useState([0, 0]), mapZoomLatLng = _j[0], setMapZoomLatLng = _j[1];
    var zoomToMarker = function (latlng) {
        setMapZoomLatLng(latlng);
    };
    var _k = useState(), positionLatLng = _k[0], setPositionLatLng = _k[1];
    var positionSet = function (latlng) {
        setPositionLatLng(latlng);
    };
    return (React.createElement("div", null,
        React.createElement(SearchBar, { searchCallback: searchCallback, filterTags: filterTags, transactionTags: transactionTags }),
        React.createElement(Map, { myPositionFitBound: false, markers: mapMarkers, viewLatLng: mapZoomLatLng, positionSet: positionSet }),
        React.createElement(Cards, { cardResponse: cardResponse, paginationCallback: paginationCallback, currentPage: currentPage, zoomToMarker: zoomToMarker, positionSetLatLng: positionLatLng, sortingCallback: sortingChange })));
};
export default StartPage;
//# sourceMappingURL=startPage.js.map