//  Primise Polyfill
require('es6-promise/auto');
//  Fetch Polyfill
import 'whatwg-fetch';
//  .find Polyfill
require('array.prototype.find').shim();
// https://www.npmjs.com/package/url-search-params-polyfill
require('url-search-params-polyfill');
import React from 'react';
import ReactDOM from 'react-dom';
import * as Cookies from 'js-cookie';
//import "bootstrap"; // 61.7K
import 'bootstrap/js/dist/collapse';
import 'bootstrap/js/dist/dropdown';
import '../sass/app.scss';
// React + Typescript
import StartPage from './startPage';
import BusinessPage from './businessPage';
if (document.getElementById('startpage-container')) {
    ReactDOM.render(React.createElement(StartPage, null), document.getElementById('startpage-container'));
}
if (document.getElementById('businesspage-container')) {
    ReactDOM.render(React.createElement(BusinessPage, null), document.getElementById('businesspage-container'));
}
// Cookie consent
if (document.getElementById('cookie-consent-btn')) {
    var btn = document.getElementById('cookie-consent-btn');
    if (btn != null) {
        btn.addEventListener('click', function () {
            var popup = document.getElementById('cookie-consent-popup');
            if (popup != null)
                popup.classList.add('d-none');
            Cookies.set('cookieconsent', '1', { expires: 365 });
        }, false);
    }
}
//# sourceMappingURL=index.js.map