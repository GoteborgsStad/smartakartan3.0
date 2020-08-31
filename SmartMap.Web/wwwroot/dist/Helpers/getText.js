export function getText(key, translations, fallback) {
    var _a;
    var value = (_a = translations.find(function (t) { return t.key == (key === null || key === void 0 ? void 0 : key.toLowerCase()); })) === null || _a === void 0 ? void 0 : _a.value;
    return value == null ? fallback : value;
}
//# sourceMappingURL=getText.js.map