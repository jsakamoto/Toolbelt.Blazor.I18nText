declare const enum PersistenceLevel {
    None,
    Session,
    SessionAndLocal,
    Cookie,
    PersistentCookie
}

type I18nTextOptions = {
    persistenceLevel: PersistenceLevel;
    cookieName: string;
    storageKey: string;
}

interface DotNetObjectRef {
    invokeMethodAsync(methodName: string, ...args: any[]): Promise<any>;
}

type Blazor = {
    addEventListener: (eventName: "enhancedload", callback: () => void) => void;
    removeEventListener: (eventName: "enhancedload", callback: () => void) => void;
}

interface Window {
    Blazor: Blazor | undefined;
}


