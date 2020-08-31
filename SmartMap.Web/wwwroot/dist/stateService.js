var StateService = /** @class */ (function () {
    function StateService() {
    }
    StateService.get = function (key) {
        try {
            if (!window.sk.state) {
                return null;
            }
            var parameters = window.sk.state;
            // Don't use "parameters.find(p => p.key === key)" because IE cries about it? Change target?
            var entry = parameters.find(function (p) { return p.key === key; });
            return entry !== undefined ? entry.value : null;
        }
        catch (_a) {
            return null;
        }
    };
    return StateService;
}());
export { StateService };
//# sourceMappingURL=stateService.js.map