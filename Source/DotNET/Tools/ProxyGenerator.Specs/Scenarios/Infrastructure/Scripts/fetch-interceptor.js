// Fetch polyfill that intercepts and stores pending requests
var __pendingFetch = null;
function fetch(url, options) {
    var urlString = (typeof url === 'object' && url.href) ? url.href : String(url);
    return new Promise(function(resolve, reject) {
        __pendingFetch = {
            url: urlString,
            options: options || {},
            resolve: resolve,
            reject: reject
        };
    });
}
