namespace Toolbelt.Blazor.I18nText {
    interface DotNetObjectRef {
        invokeMethodAsync(methodName: string, ...args: any[]): Promise<any>;
    }

    export function initLang(svcObj: DotNetObjectRef): void {
        svcObj.invokeMethodAsync('InitLang', navigator.languages || [(navigator as any).browserLanguage]);
    }
}