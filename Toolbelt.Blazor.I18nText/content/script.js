var Toolbelt;
(function (Toolbelt) {
    var Blazor;
    (function (Blazor) {
        var I18nText;
        (function (I18nText) {
            I18nText.storageKyes = {
                currentLanguage: 'Toolbelt.Blazor.I18nText.CurrentLanguage'
            };
            function initLang(svcObj, persistanceLevel) {
                const key = I18nText.storageKyes.currentLanguage;
                let lang = (persistanceLevel >= 1 ? sessionStorage.getItem(key) : null) || (persistanceLevel >= 2 ? localStorage.getItem(key) : null);
                const langs = (lang !== null ? [lang] : (navigator.languages || [navigator.browserLanguage]));
                lang = langs[0] || 'en';
                setCurrentLang(lang, persistanceLevel);
                svcObj.invokeMethodAsync('InitLang', lang);
            }
            I18nText.initLang = initLang;
            function setCurrentLang(lang, persistanceLevel) {
                const key = I18nText.storageKyes.currentLanguage;
                if (persistanceLevel >= 1)
                    sessionStorage.setItem(key, lang);
                if (persistanceLevel >= 2)
                    localStorage.setItem(key, lang);
            }
            I18nText.setCurrentLang = setCurrentLang;
        })(I18nText = Blazor.I18nText || (Blazor.I18nText = {}));
    })(Blazor = Toolbelt.Blazor || (Toolbelt.Blazor = {}));
})(Toolbelt || (Toolbelt = {}));
//# sourceMappingURL=script.js.map