namespace Toolbelt.Blazor.I18nText {
    const searchParam = document.currentScript?.getAttribute('src')?.split('?')[1] || '';
    export var ready = import('./script.module.min.js?' + searchParam).then(m => {
        Object.assign(Toolbelt.Blazor.I18nText, m.Toolbelt.Blazor.I18nText);
    });
}
