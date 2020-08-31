import React, { useState, useEffect } from 'react';
import { calculateDistance } from '../Helpers/calculateDistance';
import { getText } from '../Helpers/getText';
import { getService } from '../bussinessService';
var DefaultICardState = { cards: [], totalFound: 0, itemsPerPage: 0 };
export var DefaultSortValue = 'Random';
export var Cards = function (props) {
    var _a;
    var _b = useState(DefaultICardState), cardState = _b[0], setCardState = _b[1];
    var _c = useState(true), isLoading = _c[0], setIsLoading = _c[1];
    var _d = useState(DefaultSortValue), sortValue = _d[0], setSortValue = _d[1];
    var _e = useState(), textTranslations = _e[0], setTextTranslations = _e[1];
    useEffect(function () {
        var _service = getService();
        if (_service != null) {
            _service.getTranslations().then(function (translations) {
                setTextTranslations({
                    loadMoreCardsText: getText('cards.load-more-cards', translations, 'Ladda fler kort'),
                    loadingText: getText('common.loading', translations, 'Laddar...'),
                    digitalCardText: getText('cards.digital-card', translations, 'Digital'),
                    noCardsFound: getText('cards.no-cards-found', translations, 'Inga kort funna'),
                    sortTitle: getText('cards.sort-title', translations, 'Sortera'),
                    sortRandom: getText('cards.sort-random', translations, 'Slumpa'),
                    sortLatestAdded: getText('cards.sort-latest-added', translations, 'Senast tillagd'),
                    sortLatestUpdated: getText('cards.sort-latest-updated', translations, 'Senast uppdaterat'),
                    sortHeaderAcs: getText('cards.sort-header-acs', translations, 'Verksamhet A-Ö'),
                    sortHeaderDesc: getText('cards.sort-header-desc', translations, 'Verksamhet Ö-A'),
                    showCardsXofY: getText('cards.show-x-cards-of-y', translations, 'Visar [ShowingCards] kort av [TotalFound]'),
                });
            });
        }
    }, []);
    useEffect(function () {
        var response = props.cardResponse;
        setCardState({
            cards: response.items,
            itemsPerPage: response.itemsPerPage,
            totalFound: response.total
        });
        setIsLoading(response.itemsPerPage <= 0);
    }, [props.cardResponse]);
    var loadMoreCards = function () {
        props.paginationCallback(props.currentPage + 1);
    };
    var positionCard = function (e, latlng) {
        props.zoomToMarker(latlng);
        e.preventDefault();
    };
    var handleSortChange = function (event) {
        setSortValue(event.target.value);
        props.sortingCallback(event.target.value);
    };
    var renderCards = [];
    var renderPagination = React.createElement(React.Fragment, null);
    var renderCardsFound = React.createElement(React.Fragment, null);
    var renderLoader = React.createElement(React.Fragment, null);
    renderCards = cardState.cards.map(function (c, i) {
        var _a, _b;
        return React.createElement("div", { className: "col mb-4", key: i },
            React.createElement("a", { className: "card-link", href: c.detailPageLink },
                React.createElement("div", { className: "card h-100" },
                    React.createElement("div", { className: "position-relative" },
                        c.hasImage ? (React.createElement("img", { className: "card-img-top mr-2", src: c.imageUrl, alt: c.imageAlt })) : (React.createElement("svg", { className: "bd-placeholder-img card-img-top mr-2", width: "100%", xmlns: "http://www.w3.org/2000/svg", preserveAspectRatio: "xMidYMid slice", focusable: "false", role: "img", "aria-label": "Placeholder: Image cap" },
                            React.createElement("title", null, "Placeholder"),
                            React.createElement("rect", { width: "100%", height: "100%", fill: "#c0c5ce" }),
                            React.createElement("text", { x: "50%", y: "50%", fill: "#dee2e6", dy: ".3em" }, "Smarta Kartan"))),
                        React.createElement("div", { className: "info-over-image" },
                            (props.positionSetLatLng != null && ((_a = c.addressAndCoordinates) === null || _a === void 0 ? void 0 : _a.length) === 1) &&
                                React.createElement(React.Fragment, null,
                                    React.createElement("span", { className: "badge badge-secondary" },
                                        calculateDistance(props.positionSetLatLng[0], props.positionSetLatLng[1], c.addressAndCoordinates[0].latitude, c.addressAndCoordinates[0].longitude),
                                        " km"),
                                    "\u00A0"),
                            ((_b = c.addressAndCoordinates) === null || _b === void 0 ? void 0 : _b.length) === 1 &&
                                React.createElement("span", { onClick: function (e) { return positionCard(e, [c.addressAndCoordinates[0].latitude, c.addressAndCoordinates[0].longitude]); }, className: "badge badge-primary text-capitalize" },
                                    React.createElement("i", { className: "fas fa-map-marker-alt" })))),
                    React.createElement("div", { className: "card-body d-flex align-items-stretch flex-column" },
                        c.onlineOnly ? (React.createElement("span", { className: "font-weight-light" },
                            React.createElement("i", { className: "fas fa-globe" }),
                            " ", textTranslations === null || textTranslations === void 0 ? void 0 :
                            textTranslations.digitalCardText)) : (c.area == null || c.area.length <= 0 ? (React.createElement("span", { className: "font-weight-light" },
                            React.createElement("i", { className: "fas fa-map-marker-alt" }),
                            " ",
                            c.city)) : (React.createElement("span", { className: "font-weight-light" },
                            React.createElement("i", { className: "fas fa-map-marker-alt" }),
                            " ",
                            c.area,
                            ", ",
                            c.city))),
                        React.createElement("h3", { className: "card-title my-1" }, c.header),
                        React.createElement("p", { className: "card-text mb-auto" }, c.description)))));
    });
    if ((cardState.itemsPerPage * (props.currentPage + 1)) < cardState.totalFound) {
        renderPagination =
            React.createElement("button", { className: "btn btn-lg btn-primary btn-block", onClick: loadMoreCards }, textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.loadMoreCardsText);
    }
    if (isLoading) {
        renderLoader = React.createElement("div", null,
            React.createElement("div", { className: "text-center w-100 pt-5 h4" }, textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.loadingText),
            React.createElement("div", null, "\u00A0"));
    }
    else {
        if (cardState.cards.length <= 0) {
            renderCardsFound = React.createElement("span", { className: "align-sub cards-found" }, textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.noCardsFound);
        }
        else {
            var showingCards = (_a = textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.showCardsXofY) !== null && _a !== void 0 ? _a : "";
            showingCards = showingCards
                .replace("[ShowingCards]", cardState.cards.length.toString())
                .replace("[TotalFound]", cardState.totalFound.toString());
            renderCardsFound = React.createElement("span", { className: "align-sub cards-found" }, showingCards);
        }
    }
    return React.createElement(React.Fragment, null,
        React.createElement("div", { className: "row" },
            React.createElement("div", { className: "col-6 col-sm-9" }, renderCardsFound),
            React.createElement("div", { className: "col-6 col-sm-3 align-right" },
                React.createElement("div", { className: "form-group" },
                    React.createElement("label", { className: "sr-only", htmlFor: "search-sort" }, textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.sortTitle),
                    React.createElement("select", { value: sortValue, onChange: handleSortChange, className: "form-control form-control-sm", id: "search-sort" },
                        React.createElement("option", { value: "Random" }, textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.sortRandom),
                        React.createElement("option", { value: "LatestAdded" }, textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.sortLatestAdded),
                        React.createElement("option", { value: "LatestUpdated" }, textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.sortLatestUpdated),
                        React.createElement("option", { value: "HeaderAcs" }, textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.sortHeaderAcs),
                        React.createElement("option", { value: "HeaderDesc" }, textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.sortHeaderDesc))))),
        renderLoader,
        React.createElement("div", { className: "row row-cols-1 row-cols-md-2 row-cols-lg-3" }, renderCards),
        React.createElement("div", { className: "row" },
            React.createElement("div", { className: "col-lg-4 offset-lg-4" }, renderPagination)));
};
//# sourceMappingURL=cards.js.map