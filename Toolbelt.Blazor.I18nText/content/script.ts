namespace Toolbelt.Blazor.I18nText {
    interface DotNetObjectRef {
        invokeMethodAsync(methodName: string, ...args: any[]): Promise<any>;
    }

    const key = 'Toolbelt.Blazor.I18nText.CurrentLanguage';

    export function initLang(svcObj: DotNetObjectRef): void {
        const lang = sessionStorage.getItem(key) || localStorage.getItem(key);
        const langs = lang !== null ? [lang] : (navigator.languages || [(navigator as any).browserLanguage]);
        svcObj.invokeMethodAsync('InitLang', langs);
    }

    export function setCurrentLang(lang: string): void {
        sessionStorage.setItem(key, lang);
        localStorage.setItem(key, lang);
    }
}