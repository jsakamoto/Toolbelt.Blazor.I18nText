"use strict";
var Toolbelt;
(function (Toolbelt) {
    var Blazor;
    (function (Blazor) {
        var I18nText;
        (function (I18nText) {
            var _a, _b;
            const searchParam = ((_b = (_a = document.currentScript) === null || _a === void 0 ? void 0 : _a.getAttribute('src')) === null || _b === void 0 ? void 0 : _b.split('?')[1]) || '';
            const url = ['./script.module.min.js', searchParam].filter(v => v != '').join('?');
            I18nText.ready = import(url).then(m => {
                Object.assign(I18nText, m.Toolbelt.Blazor.I18nText);
            });
        })(I18nText = Blazor.I18nText || (Blazor.I18nText = {}));
    })(Blazor = Toolbelt.Blazor || (Toolbelt.Blazor = {}));
})(Toolbelt || (Toolbelt = {}));
