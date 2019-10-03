"use strict";
var Toolbelt;
(function (Toolbelt) {
    var Blazor;
    (function (Blazor) {
        var I18nText;
        (function (I18nText) {
            I18nText.storageKyes = {
                currentLanguage: 'Toolbelt.Blazor.I18nText.CurrentLanguage'
            };
            function initLang(persistanceLevel) {
                var key = I18nText.storageKyes.currentLanguage;
                var lang = (persistanceLevel >= 1 ? sessionStorage.getItem(key) : null) || (persistanceLevel >= 2 ? localStorage.getItem(key) : null);
                var langs = (lang !== null ? [lang] : (navigator.languages || [navigator.browserLanguage]));
                lang = langs[0] || 'en';
                return lang;
            }
            I18nText.initLang = initLang;
            function setCurrentLang(lang, persistanceLevel) {
                var key = I18nText.storageKyes.currentLanguage;
                if (persistanceLevel >= 1)
                    sessionStorage.setItem(key, lang);
                if (persistanceLevel >= 2)
                    localStorage.setItem(key, lang);
            }
            I18nText.setCurrentLang = setCurrentLang;
        })(I18nText = Blazor.I18nText || (Blazor.I18nText = {}));
    })(Blazor = Toolbelt.Blazor || (Toolbelt.Blazor = {}));
})(Toolbelt || (Toolbelt = {}));
