import React, { useState, useEffect } from 'react';
import { getText } from '../Helpers/getText';
import { getService } from '../bussinessService';
var TagType;
(function (TagType) {
    TagType[TagType["Tag"] = 0] = "Tag";
    TagType[TagType["TransactionForm"] = 1] = "TransactionForm";
})(TagType || (TagType = {}));
export var ISearchStateDefaultValue = { query: '', tags: [], transactionTags: [], digital: false, openNow: false };
export var SearchBar = function (props) {
    var _a = useState(''), queryState = _a[0], setQueryState = _a[1];
    var _b = useState(false), digitalState = _b[0], setDigitalState = _b[1];
    var _c = useState(false), openNowState = _c[0], setOpenNowState = _c[1];
    var _d = useState(), textTranslations = _d[0], setTextTranslations = _d[1];
    var _e = useState([]), transactionTagsState = _e[0], setTransactionTagsState = _e[1];
    var _f = useState(1), currentTagsPage = _f[0], setCurrentTagsPage = _f[1];
    var _g = useState([]), tagsState = _g[0], setTagsState = _g[1];
    var _h = useState([]), visibleTags = _h[0], setVisibleTags = _h[1];
    var pageSize = 6; // tags
    useEffect(function () {
        var _service = getService();
        if (_service != null) {
            _service.getTranslations().then(function (translations) {
                setTextTranslations({
                    searchButtonText: getText('searchbar.search-button', translations, 'Sök'),
                    filterButtonText: getText('searchbar.filter-button', translations, 'Filter'),
                    srBusinesses: getText('searchbar.sr-only-businesses', translations, 'antal verksamheter'),
                    moreTagsButton: getText('searchbar.more-tags-button', translations, 'Fler taggar'),
                    searchInputPlaceholderText: getText('searchbar.search-input-placeholder', translations, 'Sök på något du vill hyra, byta, låna, dela, ge eller få...'),
                    transactionHeaderText: getText('searchbar.filter-transaction-header', translations, 'Transaktionsform'),
                    miscHeaderText: getText('searchbar.filter-misc-header', translations, 'Övrigt'),
                    chkboxDigitalText: getText('searchbar.filter-checkbox-digital', translations, 'Endast digitalt'),
                    chkboxOpenNowText: getText('searchbar.filter-checkbox-opennow', translations, 'Öppet nu'),
                    searchInputAriaLabel: getText('searchbar.search-input-aria-label', translations, 'sök verksamhet')
                });
            });
        }
        var fdd = document.getElementById("filter-menu-btn");
        if (fdd != null)
            fdd.addEventListener("click", openFilterMenu);
        var b = document.getElementsByTagName("body")[0];
        b.addEventListener("click", hideFilterMenu);
        return function () {
            var fdd = document.getElementById("filter-menu-btn");
            if (fdd != null)
                fdd.removeEventListener("click", openFilterMenu);
            var b = document.getElementsByTagName("body")[0];
            b.removeEventListener("click", hideFilterMenu);
        };
    }, []);
    useEffect(function () {
        setCurrentTagsPage(1);
        //let tags = paginate(props.filterTags, pageSize, 1);
        var tags = props.filterTags;
        setVisibleTags(tags);
    }, [props.filterTags]);
    useEffect(function () {
        if (currentTagsPage > 1) {
            var tags = paginate(props.filterTags, pageSize, currentTagsPage);
            setVisibleTags(visibleTags.concat(tags));
        }
    }, [currentTagsPage]);
    var openFilterMenu = function (event) {
        var menu = document.getElementById('filter-menu');
        if (menu != null)
            menu.classList.toggle('show');
    };
    var hideFilterMenu = function (e) {
        var menu = document.getElementById('filter-menu');
        if (menu != null && e.target != null) {
            var target = e.target;
            if (menu.classList.contains("show") && !target.classList.contains("dont-hide")) {
                menu.classList.remove('show');
            }
        }
    };
    var search = function (query, tags, transactionTags, digital, openNow) {
        props.searchCallback({
            query: query,
            tags: tags,
            transactionTags: transactionTags,
            digital: digital,
            openNow: openNow,
        });
    };
    var callSearch = function () {
        search(queryState, tagsState, transactionTagsState, digitalState, openNowState);
    };
    var handleKeyDown = function (e) {
        if (e.key === 'Enter') {
            search(queryState, tagsState, transactionTagsState, digitalState, openNowState);
        }
    };
    var handleSearchChange = function (e) {
        setQueryState(e.currentTarget.value);
    };
    var removeFromTagState = function (tag, type) {
        if (type == TagType.Tag) {
            if (tagsState.indexOf(tag) !== -1) {
                var tags = tagsState.filter(function (e) { return e !== tag; });
                setTagsState(tags);
                search(queryState, tags, transactionTagsState, digitalState, openNowState);
            }
        }
        else if (type == TagType.TransactionForm) {
            if (transactionTagsState.indexOf(tag) !== -1) {
                var tags = transactionTagsState.filter(function (e) { return e !== tag; });
                setTransactionTagsState(tags);
                search(queryState, tagsState, tags, digitalState, openNowState);
            }
        }
    };
    var addToTagState = function (tag, type) {
        if (type == TagType.Tag) {
            if (tagsState.indexOf(tag) === -1) {
                var tags = tagsState.concat([tag]);
                setTagsState(tags);
                search(queryState, tags, transactionTagsState, digitalState, openNowState);
            }
        }
        else if (type == TagType.TransactionForm) {
            if (transactionTagsState.indexOf(tag) === -1) {
                var tags = transactionTagsState.concat([tag]);
                setTransactionTagsState(tags);
                search(queryState, tagsState, tags, digitalState, openNowState);
            }
        }
    };
    var addRemoveFromTags = function (tag) {
        if (tagsState.indexOf(tag) === -1) {
            addToTagState(tag, TagType.Tag);
        }
        else {
            removeFromTagState(tag, TagType.Tag);
        }
    };
    var tagClick = function (e) {
        addRemoveFromTags(e.currentTarget.value);
    };
    var changeEvent = function (e) {
        var isChecked = e.currentTarget.checked;
        var value = e.currentTarget.value;
        var id = e.currentTarget.id;
        if (id.indexOf('cct-') !== -1) {
            if (isChecked)
                addToTagState(value, TagType.TransactionForm);
            else
                removeFromTagState(value, TagType.TransactionForm);
        }
        if (id.indexOf('cc-') !== -1) {
            if (id === 'cc-digital') {
                console.log('digital', isChecked);
                setDigitalState(isChecked);
                search(queryState, tagsState, transactionTagsState, isChecked, openNowState);
            }
            if (id === 'cc-opennow') {
                console.log('opennow', isChecked);
                setOpenNowState(isChecked);
                search(queryState, tagsState, transactionTagsState, digitalState, isChecked);
            }
        }
    };
    // url: https://stackoverflow.com/a/42761393
    var paginate = function (array, page_size, page_number) {
        // human-readable page numbers usually start with 1, so we reduce 1 in the first argument
        return array.slice((page_number - 1) * page_size, page_number * page_size);
    };
    var moreTags = function (page) {
        setCurrentTagsPage(page);
    };
    // const result = words.filter(word => word.length > 6);
    var renderTags = React.createElement("div", null, visibleTags.filter(function (t) { return t.type == "main"; }).map(function (t, i) {
        return React.createElement("span", { key: t.id },
            React.createElement("button", { type: "button", value: t.name, onClick: tagClick, className: "btn " + (tagsState.indexOf(t.name) === -1 ? 'btn-outline-primary' : 'btn-primary') + " btn-sm text-capitalize mb-2" },
                t.name,
                React.createElement("span", { className: "sr-only" }, textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.srBusinesses)),
            "\u00A0");
    }));
    var renderSubTags = React.createElement("div", null, visibleTags.filter(function (t) { return t.type == "sub"; }).map(function (t, i) {
        return React.createElement("span", { key: t.id },
            React.createElement("button", { type: "button", value: t.name, onClick: tagClick, className: "btn " + (tagsState.indexOf(t.name) === -1 ? 'btn-outline-info' : 'btn-info') + " btn-sm text-capitalize mb-2" },
                t.name,
                React.createElement("span", { className: "sr-only" }, textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.srBusinesses)),
            "\u00A0");
    }));
    var renderTransactionTags = props.transactionTags.map(function (t, i) {
        return React.createElement("div", { className: "custom-control custom-checkbox mb-1 dont-hide", key: t.id },
            React.createElement("input", { type: "checkbox", onChange: changeEvent, className: "custom-control-input dont-hide", id: "cct-" + t.id, name: "cct-" + t.id, value: t.name }),
            React.createElement("label", { className: "custom-control-label text-capitalize dont-hide", htmlFor: "cct-" + t.id }, t.name));
    });
    return React.createElement("div", { className: "searchbar-component" },
        React.createElement("div", { className: "input-group mb-2" },
            React.createElement("input", { type: "text", className: "form-control", placeholder: textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.searchInputPlaceholderText, "aria-label": textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.searchInputAriaLabel, "aria-describedby": "button-search", onChange: handleSearchChange, onKeyDown: handleKeyDown, value: queryState }),
            React.createElement("div", { className: "input-group-append" },
                React.createElement("button", { className: "btn btn-primary", type: "button", id: "button-search", onClick: callSearch },
                    React.createElement("i", { className: "fas fa-search" }),
                    " ", textTranslations === null || textTranslations === void 0 ? void 0 :
                    textTranslations.searchButtonText),
                React.createElement("button", { type: "button", id: "filter-menu-btn", className: "btn btn-primary dropdown-toggle btn-sm dont-hide", "data-display": "static", "aria-haspopup": "true", "aria-expanded": "false" },
                    React.createElement("i", { className: "fas fa-filter dont-hide" }),
                    " ", textTranslations === null || textTranslations === void 0 ? void 0 :
                    textTranslations.filterButtonText),
                React.createElement("div", { className: "dropdown-menu dropdown-menu-right dont-hide", id: "filter-menu" },
                    React.createElement("div", { className: "px-4 py-3 dont-hide" },
                        React.createElement("div", { className: "row dont-hide" },
                            React.createElement("div", { className: "col-6 dont-hide" },
                                React.createElement("h6", { className: "dont-hide" }, textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.transactionHeaderText),
                                React.createElement("div", { className: "form-group pt-1 dont-hide" }, renderTransactionTags)),
                            React.createElement("div", { className: "col-6 dont-hide" },
                                React.createElement("h6", { className: "dont-hide" }, textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.miscHeaderText),
                                React.createElement("div", { className: "form-group pt-1 dont-hide" },
                                    React.createElement("div", { className: "custom-control custom-checkbox mb-1 dont-hide" },
                                        React.createElement("input", { type: "checkbox", onChange: changeEvent, className: "custom-control-input dont-hide", id: "cc-digital", name: "cc-digital", value: "digital" }),
                                        React.createElement("label", { className: "custom-control-label text-capitalize dont-hide", htmlFor: "cc-digital" }, textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.chkboxDigitalText)),
                                    React.createElement("div", { className: "custom-control custom-checkbox dont-hide" },
                                        React.createElement("input", { type: "checkbox", onChange: changeEvent, className: "custom-control-input dont-hide", id: "cc-opennow", name: "cc-opennow", value: "opennow" }),
                                        React.createElement("label", { className: "custom-control-label text-capitalize dont-hide", htmlFor: "cc-opennow" }, textTranslations === null || textTranslations === void 0 ? void 0 : textTranslations.chkboxOpenNowText))))))))),
        React.createElement("div", null, renderTags),
        React.createElement("div", null, renderSubTags));
};
//# sourceMappingURL=searchBar.js.map