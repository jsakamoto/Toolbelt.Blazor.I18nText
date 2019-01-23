var Toolbelt;
(function (Toolbelt) {
    var Blazor;
    (function (Blazor) {
        var I18nText;
        (function (I18nText) {
            const key = 'Toolbelt.Blazor.I18nText.CurrentLanguage';
            function initLang(svcObj) {
                const lang = sessionStorage.getItem(key) || localStorage.getItem(key);
                const langs = lang !== null ? [lang] : (navigator.languages || [navigator.browserLanguage]);
                svcObj.invokeMethodAsync('InitLang', langs);
            }
            I18nText.initLang = initLang;
            function setCurrentLang(lang) {
                sessionStorage.setItem(key, lang);
                localStorage.setItem(key, lang);
            }
            I18nText.setCurrentLang = setCurrentLang;
        })(I18nText = Blazor.I18nText || (Blazor.I18nText = {}));
    })(Blazor = Toolbelt.Blazor || (Toolbelt.Blazor = {}));
})(Toolbelt || (Toolbelt = {}));
//# sourceMappingURL=script.js.map