// Set up Arc Globals for origin and API base path
// Set up Arc Globals by loading and modifying the Globals module
try {
    var ArcModule = require('@cratis/arc');
    if (ArcModule && ArcModule.Globals) {
        ArcModule.Globals.origin = '{{ORIGIN}}';
        ArcModule.Globals.apiBasePath = '';
        ArcModule.Globals.microservice = '';
    }
    
    // Also set on globalThis for compatibility
    if (!globalThis.Globals) {
        globalThis.Globals = ArcModule.Globals;
    } else {
        globalThis.Globals.origin = '{{ORIGIN}}';
        globalThis.Globals.apiBasePath = '';
        globalThis.Globals.microservice = '';
    }
    
    // CRITICAL: Expose TimeSpan from fundamentals globally for command serialization
    var Fundamentals = require('@cratis/fundamentals');
    if (Fundamentals && Fundamentals.TimeSpan) {
        globalThis.TimeSpan = Fundamentals.TimeSpan;
    }
} catch (e) {
    console.error('Failed to setup Arc Globals:', e.message);
    throw e;
}
