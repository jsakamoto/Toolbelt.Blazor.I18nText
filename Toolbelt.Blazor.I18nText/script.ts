namespace Toolbelt.Blazor.I18nText {
    const searchParam = document.currentScript?.getAttribute('src')?.split('?')[1] || '';
    const url = ['./script.module.min.js', searchParam].filter(v => v != '').join('?');
    export var ready = import(url).then(m => {
        Object.assign(I18nText, m.Toolbelt.Blazor.I18nText);
    });
}
