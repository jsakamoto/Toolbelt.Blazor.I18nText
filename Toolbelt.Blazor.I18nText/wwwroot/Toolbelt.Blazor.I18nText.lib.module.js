export const beforeServerStart = (options) => {
    const circuitHandlers = options.circuitHandlers;
    circuitHandlers.push({ onCircuitClosed: () => document.dispatchEvent(new Event('Toolbelt.Blazor.circuitClosed')) });
}