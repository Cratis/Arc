// Patch TimeSpan values in command result from HTTP response
try {
    var httpResponse = JSON.parse('{{ESCAPED_RESPONSE}}');
    if (httpResponse.response && __cmdResult.response) {
        // Iterate through all properties in the HTTP response
        for (var key in httpResponse.response) {
            var value = httpResponse.response[key];
            // Check if it's a TimeSpan string that needs re-parsing
            if (typeof value === 'string' && /^(-)?(?:(\d+)\.)?(\d{1,2}):(\d{2}):(\d{2})(?:\.(\d{1,7}))?$/.test(value)) {
                if (globalThis.TimeSpan && globalThis.TimeSpan.parse) {
                    __cmdResult.response[key] = globalThis.TimeSpan.parse(value);
                }
            }
        }
    }
} catch(e) {
    // Ignore TimeSpan patch errors
}
