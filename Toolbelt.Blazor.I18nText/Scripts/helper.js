const blazor = window.Blazor;
const doc = document;
const enhancedload = "enhancedload";
const circuitClosed = "Toolbelt.Blazor.circuitClosed";
const localstorage = localStorage;
const sessionstorage = sessionStorage;
const getLangFromCookie = (cookieName) => decodeURIComponent(doc.cookie.split(/[; ]+/).map(e => e.split('=')).filter(e => e[0] === cookieName)[0]?.pop() || '')
    .match(/\|uic=(.+)$/)?.pop() || null;
const getCurrentLang = ({ persistenceLevel, cookieName, storageKey }) => {
    const langFromSessionStorage = sessionstorage.getItem(storageKey);
    const langFromLocalStorage = localstorage.getItem(storageKey);
    const langFromCookie = getLangFromCookie(cookieName);
    const lang = ([1, 2].includes(persistenceLevel) ? langFromSessionStorage : null)
        ||
            (persistenceLevel === 2 ? langFromLocalStorage : null)
        ||
            ([3, 4].includes(persistenceLevel) ? langFromCookie : null);
    const langs = (lang ? [lang] : (navigator.languages || [navigator.browserLanguage]));
    return langs[0] || 'en';
};
const setCurrentLang = (lang, { persistenceLevel, cookieName, storageKey }) => {
    if ([1, 2].includes(persistenceLevel))
        sessionstorage.setItem(storageKey, lang);
    if (persistenceLevel === 2)
        localstorage.setItem(storageKey, lang);
    if ([3, 4].includes(persistenceLevel))
        doc.cookie = `${cookieName}=${encodeURIComponent(`c=${lang}|uic=${lang}`)}` + (persistenceLevel === 4) ? "; expires=Fri, 31 Dec 9999 23:59:59 GMT;" : "";
};
export const attach = async (dotNetObjectRef, options) => {
    const helper = {
        currentLang: getCurrentLang(options),
        getCurrentLang: () => getCurrentLang(options),
        setCurrentLang: (lang) => setCurrentLang(lang, options),
    };
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
    };
    blazor?.addEventListener(enhancedload, onEnhancedLoad);
    const onCircuitClosed = () => helper.dispose?.();
    doc.addEventListener(circuitClosed, onCircuitClosed);
    helper.dispose = () => {
        blazor?.removeEventListener(enhancedload, onEnhancedLoad);
        doc.removeEventListener(circuitClosed, onCircuitClosed);
    };
    return helper;
};
