// Create command instance and set properties, then invoke the specified method
var __cmd = new {{COMMAND_CLASS}}();
{{PROPERTY_ASSIGNMENTS}}
var __cmdResult = null;
var __cmdError = null;
var __cmdDone = false;
__cmd.{{METHOD_NAME}}().then(function(result) {
    __cmdResult = result;
    __cmdDone = true;
}).catch(function(error) {
    __cmdError = error;
    __cmdDone = true;
});
