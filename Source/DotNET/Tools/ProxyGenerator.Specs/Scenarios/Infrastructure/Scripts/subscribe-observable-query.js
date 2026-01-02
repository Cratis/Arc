// Create query instance, set parameters, and subscribe for observable query
var __obsQuery = new {{QUERY_CLASS}}();
{{PARAMETER_ASSIGNMENTS}}
// Initialize per-subscription storage
if (!globalThis.__obsSubscriptions) {
    globalThis.__obsSubscriptions = {};
}
globalThis.__obsSubscriptions['{{SUBSCRIPTION_ID}}'] = { updates: [] };
var __obsError = null;
var __obsSubscription = __obsQuery.subscribe(function(result) {
    globalThis.__obsSubscriptions['{{SUBSCRIPTION_ID}}'].updates.push(result);
    // Notify C# that an update has been received, passing the subscription ID
    if (typeof globalThis.__notifyObservableUpdate === 'function') {
        globalThis.__notifyObservableUpdate('{{SUBSCRIPTION_ID}}');
    }
}, __obsQuery);
