var Toolbelt;
(function (Toolbelt) {
    var Blazor;
    (function (Blazor) {
        var I18nText;
        (function (I18nText) {
            function initLang(svcObj) {
                svcObj.invokeMethodAsync('InitLang', navigator.languages || [navigator.browserLanguage]);
            }
            I18nText.initLang = initLang;
        })(I18nText = Blazor.I18nText || (Blazor.I18nText = {}));
    })(Blazor = Toolbelt.Blazor || (Toolbelt.Blazor = {}));
})(Toolbelt || (Toolbelt = {}));
//# sourceMappingURL=script.js.map