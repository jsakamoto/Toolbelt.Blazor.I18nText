
const handlers = new Set();

export const circuitClosed = {
    add: (handler) => handlers.add(handler),
    remove: (handler) => handlers.delete(handler)
}

export const beforeServerStart = (options) => {
    const circuitHandlers = options.circuitHandlers;
    circuitHandlers.splice(circuitHandlers.length, 0, {
        onCircuitClosed: () => handlers.forEach(handler => handler?.())
    });
}