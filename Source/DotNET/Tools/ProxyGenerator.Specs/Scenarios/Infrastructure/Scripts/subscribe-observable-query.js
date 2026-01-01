// Create query instance, set parameters, and subscribe for observable query
var __obsQuery = new {{QUERY_CLASS}}();
{{PARAMETER_ASSIGNMENTS}}
var __obsUpdates = [];
var __obsError = null;
var __obsSubscription = __obsQuery.subscribe(function(result) {
    __obsUpdates.push(result);
}, __obsQuery);
