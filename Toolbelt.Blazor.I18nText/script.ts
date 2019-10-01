namespace Toolbelt.Blazor.I18nText {
    interface DotNetObjectRef {
        invokeMethodAsync(methodName: string, ...args: any[]): Promise<any>;
    }

    export var storageKyes = {
        currentLanguage: 'Toolbelt.Blazor.I18nText.CurrentLanguage'
    };

    const enum PersistanceLevel {
        None,
        Session,
        SessionAndLocal
    }

    export function initLang(persistanceLevel: PersistanceLevel): string {
        const key = storageKyes.currentLanguage;
        let lang = (persistanceLevel >= PersistanceLevel.Session ? sessionStorage.getItem(key) : null) || (persistanceLevel >= PersistanceLevel.SessionAndLocal ? localStorage.getItem(key) : null);
        const langs = (lang !== null ? [lang] : (navigator.languages || [(navigator as any).browserLanguage])) as string[];

        lang = langs[0] || 'en';
        return lang;
    }

    export function setCurrentLang(lang: string, persistanceLevel: PersistanceLevel): void {
        const key = storageKyes.currentLanguage;
        if (persistanceLevel >= PersistanceLevel.Session) sessionStorage.setItem(key, lang);
        if (persistanceLevel >= PersistanceLevel.SessionAndLocal) localStorage.setItem(key, lang);
    }
}