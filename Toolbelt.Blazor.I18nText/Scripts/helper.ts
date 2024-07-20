const blazor = window.Blazor;
const doc = document;
const enhancedload = "enhancedload";
const localstorage = localStorage;
const sessionstorage = sessionStorage;

type Helper = {
    currentLang: string;
    getCurrentLang: () => string;
    setCurrentLang: (lang: string) => void;
    dispose?: () => void;
}


const getLangFromCookie = (cookieName: string): string | null =>
    decodeURIComponent(doc.cookie.split(/[; ]+/).map(e => e.split('=')).filter(e => e[0] === cookieName)[0]?.pop() || '')
    .match(/\|uic=(.+)$/)?.pop() || null;

const getCurrentLang = ({ persistenceLevel, cookieName, storageKey }: I18nTextOptions): string => {
    const langFromSessionStorage = sessionstorage.getItem(storageKey);
    const langFromLocalStorage = localstorage.getItem(storageKey);
    const langFromCookie = getLangFromCookie(cookieName);

    const lang =
        ([PersistenceLevel.Session, PersistenceLevel.SessionAndLocal].includes(persistenceLevel) ? langFromSessionStorage : null)
        ||
        (persistenceLevel === PersistenceLevel.SessionAndLocal ? langFromLocalStorage : null)
        ||
        ([PersistenceLevel.Cookie, PersistenceLevel.PersistentCookie].includes(persistenceLevel) ? langFromCookie : null)
        ;
    const langs = (lang ? [lang] : (navigator.languages || [(navigator as any).browserLanguage])) as string[];

    return langs[0] || 'en';
}

const setCurrentLang = (lang: string, { persistenceLevel, cookieName, storageKey }: I18nTextOptions): void => {

    if ([PersistenceLevel.Session, PersistenceLevel.SessionAndLocal].includes(persistenceLevel)) sessionstorage.setItem(storageKey, lang);
    if (persistenceLevel === PersistenceLevel.SessionAndLocal) localstorage.setItem(storageKey, lang);
    if ([PersistenceLevel.Cookie, PersistenceLevel.PersistentCookie].includes(persistenceLevel))
        doc.cookie = `${cookieName}=${encodeURIComponent(`c=${lang}|uic=${lang}`)}` + (persistenceLevel === PersistenceLevel.PersistentCookie) ? "; expires=Fri, 31 Dec 9999 23:59:59 GMT;" : "";
}

export const attach = async (dotNetObjectRef: DotNetObjectRef, options: I18nTextOptions) => {

    const helper: Helper = {
        currentLang: getCurrentLang(options),
        getCurrentLang: () => getCurrentLang(options),
        setCurrentLang: (lang: string) => setCurrentLang(lang, options),
    }

    const onEnhancedLoad = () => {
        const currentLang = getCurrentLang(options);
        if (helper.currentLang !== currentLang) {
            helper.currentLang = currentLang;
            try {
                dotNetObjectRef.invokeMethodAsync('OnLanguageChanged', currentLang);
            }
            catch (e) {
                console.error('Error invoking OnLanguageChanged', e);
            }
        }
    }

    blazor?.addEventListener(enhancedload, onEnhancedLoad);

    const onCircuitClosed = () => helper.dispose?.();

    const { circuitClosed } = await import("./Toolbelt.Blazor.I18nText.lib.module.js");
    circuitClosed.add(onCircuitClosed);

    helper.dispose = () => {
        blazor?.removeEventListener(enhancedload, onEnhancedLoad);
        circuitClosed.remove(onCircuitClosed);
    }

    return helper;
}

