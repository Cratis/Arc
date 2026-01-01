// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Bridges the JavaScript runtime with an HTTP client for testing proxies end-to-end.
/// Intercepts fetch calls from JavaScript and routes them to the test HTTP client.
/// Provides WebSocket simulation for observable queries.
/// </summary>
public sealed class JavaScriptHttpBridge : IDisposable
{
    readonly JsonSerializerOptions _jsonOptions;
    readonly Dictionary<string, WebSocketConnection> _webSockets = new();
    readonly object _webSocketLock = new();
    readonly string _serverUrl;
    int _nextWebSocketId;
    bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="JavaScriptHttpBridge"/> class.
    /// </summary>
    /// <param name="runtime">The JavaScript runtime.</param>
    /// <param name="httpClient">The HTTP client to use for requests.</param>
    /// <param name="serverUrl">The base URL of the server.</param>
    public JavaScriptHttpBridge(JavaScriptRuntime runtime, HttpClient httpClient, string serverUrl)
    {
        Runtime = runtime;
        HttpClient = httpClient;
        _serverUrl = serverUrl;
        _jsonOptions = Json.Globals.JsonSerializerOptions;
        SetupFetchInterceptor();
        SetupWebSocketPolyfill();
        SetupArcGlobals();
    }

    /// <summary>
    /// Gets the JavaScript runtime.
    /// </summary>
    public JavaScriptRuntime Runtime { get; }

    /// <summary>
    /// Gets the HTTP client.
    /// </summary>
    public HttpClient HttpClient { get; }

    /// <summary>
    /// Loads TypeScript code into the runtime by transpiling it first.
    /// </summary>
    /// <param name="typeScriptCode">The TypeScript code to load.</param>
    /// <param name="wrapInModuleScope">Whether to wrap in an IIFE to prevent variable collisions. Set to false for query/command proxies that need global scope.</param>
    public void LoadTypeScript(string typeScriptCode, bool wrapInModuleScope = true)
    {
        var jsCode = Runtime.TranspileTypeScript(typeScriptCode);

        if (wrapInModuleScope)
        {
            // Wrap in module scope to prevent variable collisions when multiple files
            // import from the same modules (e.g., @cratis/fundamentals), but still export
            // classes to global scope so they're accessible.
            // IMPORTANT: Don't create a local require variable - use globalThis.require
            // to ensure module singletons are maintained across all loaded code.
#pragma warning disable MA0136 // Raw String contains an implicit end of line character
            var wrappedCode = $$"""
(function() {
    var module = { exports: {} };
    var exports = module.exports;
    // CRITICAL: Use globalThis.require instead of creating a local variable
    // This ensures @cratis/fundamentals is loaded as a singleton across all code
    var require = globalThis.require;
    {{jsCode}}
    // Export any classes to global scope
    for (var key in module.exports) {
        if (module.exports.hasOwnProperty(key)) {
            globalThis[key] = module.exports[key];
        }
    }
})();
""";
#pragma warning restore MA0136 // Raw String contains an implicit end of line character
            Runtime.Execute(wrappedCode);
        }
        else
        {
            Runtime.Execute(jsCode);
        }
    }

    /// <summary>
    /// Executes a command through its JavaScript proxy class.
    /// The proxy's execute() method will call fetch(), which is intercepted and routed to HTTP.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="command">The command object.</param>
    /// <returns>The command execution result.</returns>
    /// <exception cref="JavaScriptProxyExecutionFailed">The exception that is thrown when the proxy execution fails.</exception>
    public async Task<CommandExecutionResult<TResult>> ExecuteCommandViaProxyAsync<TResult>(object command)
        => await ExecuteCommandViaProxyAsync<TResult>(command, command.GetType().Name);

    /// <summary>
    /// Executes a command through its JavaScript proxy class with an explicit class name.
    /// The proxy's execute() method will call fetch(), which is intercepted and routed to HTTP.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="command">The command object.</param>
    /// <param name="commandClassName">The JavaScript class name to use.</param>
    /// <returns>The command execution result.</returns>
    /// <exception cref="JavaScriptProxyExecutionFailed">The exception that is thrown when the proxy execution fails.</exception>
    public async Task<CommandExecutionResult<TResult>> ExecuteCommandViaProxyAsync<TResult>(object command, string commandClassName)
    {
        var commandAsDocument = JsonSerializer.SerializeToDocument(command, _jsonOptions);
        var properties = new Dictionary<string, object>();
        foreach (var prop in commandAsDocument.RootElement.EnumerateObject())
        {
            // Convert JsonElement to actual value
            properties[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString()!,
                JsonValueKind.Number => prop.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null!,
                JsonValueKind.Object => prop.Value.Deserialize<Dictionary<string, object>>(_jsonOptions)!,
                JsonValueKind.Array => prop.Value.Deserialize<List<object>>(_jsonOptions)!,
                _ => prop.Value.ToString()
            };
        }

        // Set up properties on the command
        var propAssignments = string.Concat(properties.Select(p =>
        {
            var jsonValue = JsonSerializer.Serialize(p.Value, _jsonOptions);
            return $"__cmd.{p.Key} = {jsonValue};";
        }));

        // Create command instance and set properties, then call execute()
        Runtime.Execute(
            "var __cmd = new " + commandClassName + "();" +
            propAssignments +
            "var __cmdResult = null;" +
            "var __cmdError = null;" +
            "var __cmdDone = false;" +
            "__cmd.execute().then(function(result) {" +
            "    __cmdResult = result;" +
            "    __cmdDone = true;" +
            "}).catch(function(error) {" +
            "    __cmdError = error;" +
            "    __cmdDone = true;" +
            "});");

        // Check if there's a pending fetch (client-side validation might prevent roundtrip)
        var hasPendingFetch = Runtime.Evaluate<bool>("__pendingFetch !== null");
        FetchResult? result = null;

        if (hasPendingFetch)
        {
            // Process the pending fetch request
            result = await ProcessPendingFetchAsync();
        }

        // Wait for promise resolution
        SpinWait.SpinUntil(() => (bool)Runtime.Evaluate("__cmdDone")!, TimeSpan.FromSeconds(5));

        var hasError = Runtime.Evaluate<bool>("__cmdError !== null");
        if (hasError)
        {
            var errorMsg = Runtime.Evaluate<string>("__cmdError?.message || String(__cmdError)");
            throw new JavaScriptProxyExecutionFailed($"Command execution failed: {errorMsg}");
        }

        // Get the result from JavaScript
        var resultJson = Runtime.Evaluate<string>("JSON.stringify(__cmdResult)") ?? "{}";
        
        // PATCH: Manually fix TimeSpan deserialization from HTTP response
        // The JsonSerializer in the test environment doesn't properly deserialize TimeSpans
        // from the HTTP response. We need to re-parse them from the original HTTP response.
        if (result?.ResponseJson is not null)
        {
            var escapedResponse = result.ResponseJson.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r");
            Runtime.Execute($@"
                try {{
                    var httpResponse = JSON.parse('{escapedResponse}');
                    if (httpResponse.response && __cmdResult.response) {{
                        // Iterate through all properties in the HTTP response
                        for (var key in httpResponse.response) {{
                            var value = httpResponse.response[key];
                            // Check if it's a TimeSpan string that needs re-parsing
                            if (typeof value === 'string' && /^(-)?(?:(\d+)\.)?(\d{{1,2}}):(\d{{2}}):(\d{{2}})(?:\.(\d{{1,7}}))?$/.test(value)) {{
                                if (globalThis.TimeSpan && globalThis.TimeSpan.parse) {{
                                    __cmdResult.response[key] = globalThis.TimeSpan.parse(value);
                                }}
                            }}
                        }}
                    }}
                }} catch(e) {{
                    // Ignore TimeSpan patch errors
                }}
            ");
        }
        
        // Get the patched result
        resultJson = Runtime.Evaluate<string>("JSON.stringify(__cmdResult)") ?? "{}";
        
        var commandResult = JsonSerializer.Deserialize<Commands.CommandResult<TResult>>(resultJson, _jsonOptions);

        return new CommandExecutionResult<TResult>(commandResult, result?.ResponseJson);
    }

    /// <summary>
    /// Performs a query through its JavaScript proxy class.
    /// The proxy's perform() method will call fetch(), which is intercepted and routed to HTTP.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="queryClassName">The name of the query class.</param>
    /// <param name="parameters">Optional parameter values for the query.</param>
    /// <returns>The query execution result.</returns>
    /// <exception cref="JavaScriptProxyExecutionFailed">The exception that is thrown when the proxy execution fails.</exception>
    public async Task<QueryExecutionResult<TResult>> PerformQueryViaProxyAsync<TResult>(
        string queryClassName,
        Dictionary<string, object>? parameters = null)
    {
        // Build args object for perform() call
        var argsObject = parameters is not null && parameters.Any()
            ? JsonSerializer.Serialize(parameters, _jsonOptions)
            : "undefined";

        // Create query instance and call perform() with args
        var queryScript =
            "var __query = new " + queryClassName + "();" +
            "var __args = " + argsObject + ";" +
            "var __queryResult = null;" +
            "var __queryError = null;" +
            "var __queryDone = false;" +
            "var __patchApplied = false;" +
            "__query.perform(__args).then(function(result) {" +
            "    __queryResult = result;" +
            "    if (__queryResult && __queryResult.data && typeof __queryResult.data === 'object') {" +
            "        if (__query.modelType && typeof globalThis.JsonSerializer !== 'undefined') {" +
            "            try {" +
            "                var rawData = JSON.parse(JSON.stringify(__queryResult.data));" +
            "                if (Object.keys(rawData).length > 0) {" +
            "                    __queryResult.data = globalThis.JsonSerializer.deserializeFromInstance(__query.modelType, rawData);" +
            "                    __patchApplied = true;" +
            "                }" +
            "            } catch(e) {" +
            "                console.error('[Patch Error]', e.message);" +
            "            }" +
            "        }" +
            "    }" +
            "    __queryDone = true;" +
            "}).catch(function(error) {" +
            "    __queryError = error;" +
            "    __queryDone = true;" +
            "});";
        
        try
        {
            Runtime.Execute(queryScript);
        }
        catch
        {
            throw;
        }

        // Check if there's a pending fetch (client-side validation might prevent roundtrip)
        var hasPendingFetch = Runtime.Evaluate<bool>("__pendingFetch !== null");
        FetchResult? result = null;
        string? requestUrl = null;

        if (hasPendingFetch)
        {
            // Capture the URL before processing
            requestUrl = Runtime.Evaluate<string>("__pendingFetch.url");
            
            // Process the pending fetch request
            result = await ProcessPendingFetchAsync();
        }
        else
        {
            // No pending fetch (likely validation failed), but we can still construct the URL for testing
            // Extract query route and build URL with parameters
            var hasRoute = Runtime.Evaluate<bool>("__query && typeof __query.route === 'string'");
            if (hasRoute)
            {
                var route = Runtime.Evaluate<string>("__query.route");
                var queryParams = parameters?.Select(p => $"{p.Key}={Uri.EscapeDataString(JsonSerializer.Serialize(p.Value, _jsonOptions).Trim('"'))}");
                requestUrl = route + (queryParams?.Any() == true ? "?" + string.Join("&", queryParams) : "");
            }
        }

        // Wait for promise resolution
        SpinWait.SpinUntil(() => (bool)Runtime.Evaluate("__queryDone")!, TimeSpan.FromSeconds(5));

        var hasError = Runtime.Evaluate<bool>("__queryError !== null");
        if (hasError)
        {
            var errorMsg = Runtime.Evaluate<string>("__queryError?.message || String(__queryError)");
            throw new JavaScriptProxyExecutionFailed($"Query execution failed: {errorMsg}");
        }

        // PATCH: Manually deserialize the query result data if it exists
        // QueryResult doesn't properly call JsonSerializer in test environment due to module isolation
        if (result?.ResponseJson is not null)
        {
            // Debug: Save raw response for complex queries
            if (queryClassName.Contains("Complex"))
            {
                File.WriteAllText("/tmp/complex-raw-response.json", result.ResponseJson);
            }

            // Escape the response JSON for embedding in JavaScript string
            var escapedJson = result.ResponseJson.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r");
            Runtime.Execute($@"
                try {{
                    var response = JSON.parse('{escapedJson}');
                    
                    if (response.data && __query.modelType && globalThis.Fields) {{
                        // Helper function to deserialize an object using its type's fields
                        function deserializeObject(data, type) {{
                            if (!data || !type || !globalThis.Fields) return data;
                            
                            var fields = globalThis.Fields.getFieldsForType(type);
                            if (!fields || fields.length === 0) {{
                                return data;
                            }}
                            
                            var deserialized = {{}};
                            for (var i = 0; i < fields.length; i++) {{
                                var field = fields[i];
                                if (data.hasOwnProperty(field.name)) {{
                                    var value = data[field.name];
                                    
                                    // Handle null/undefined
                                    if (value === null || value === undefined) {{
                                        deserialized[field.name] = value;
                                    }}
                                    // Handle arrays
                                    else if (Array.isArray(value) && field.type && globalThis[field.type.name]) {{
                                        deserialized[field.name] = value.map(item => 
                                            typeof item === 'object' && item !== null 
                                                ? deserializeObject(item, field.type) 
                                                : item
                                        );
                                    }}
                                    // Handle nested objects
                                    else if (typeof value === 'object' && !Array.isArray(value) && field.type && globalThis[field.type.name]) {{
                                        deserialized[field.name] = deserializeObject(value, field.type);
                                    }}
                                    // Handle primitives
                                    else {{
                                        deserialized[field.name] = value;
                                    }}
                                }}
                            }}
                            return deserialized;
                        }}
                        
                        __queryResult.data = deserializeObject(response.data, __query.modelType);
                    }}
                }} catch(e) {{
                    // Ignore deserialization errors
                }}
            ");
        }

        // Get the result from JavaScript
        var resultJson = Runtime.Evaluate<string>("JSON.stringify(__queryResult)") ?? "{}";
        var queryResult = JsonSerializer.Deserialize<Queries.QueryResult>(resultJson, _jsonOptions);

        return new QueryExecutionResult<TResult>(queryResult, result?.ResponseJson, requestUrl ?? result?.Url);
    }

    /// <summary>
    /// Performs an observable query through its JavaScript proxy class.
    /// The proxy's subscribe() method opens a WebSocket connection for real-time updates.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="queryClassName">The name of the query class.</param>
    /// <param name="parameters">Optional parameter values for the query.</param>
    /// <returns>The observable query execution result with updates.</returns>
    /// <exception cref="JavaScriptProxyExecutionFailed">The exception that is thrown when the proxy execution fails.</exception>
    public async Task<ObservableQueryExecutionResult<TResult>> PerformObservableQueryViaProxyAsync<TResult>(
        string queryClassName,
        object? parameters = null)
    {
        var paramDict = parameters is not null
            ? JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(parameters, _jsonOptions),
                _jsonOptions)
            : null;

        // Set up parameters on the query
        var paramAssignments = paramDict is not null
            ? string.Concat(paramDict.Select(p => $"__obsQuery.{p.Key} = {JsonSerializer.Serialize(p.Value, _jsonOptions)};"))
            : string.Empty;

        // Create query instance, set parameters, and subscribe
        Runtime.Execute(
            "var __obsQuery = new " + queryClassName + "();" +
            paramAssignments +
            "var __obsUpdates = [];" +
            "var __obsError = null;" +
            "var __obsSubscription = __obsQuery.subscribe(function(result) {" +
            "    __obsUpdates.push(result);" +
            "}, __obsQuery);");

        // Wait for the initial WebSocket message
        var timeout = TimeSpan.FromSeconds(10);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            var hasUpdates = Runtime.Evaluate<bool>("__obsUpdates.length > 0");
            var error = Runtime.Evaluate<string>("__obsError ? JSON.stringify(__obsError) : null");

            if (error is not null)
            {
                throw new JavaScriptProxyExecutionFailed($"Observable query failed: {error}");
            }

            if (hasUpdates)
            {
                break;
            }
            await Task.Delay(50);
        }

        var hasAnyUpdates = Runtime.Evaluate<bool>("__obsUpdates.length > 0");
        if (!hasAnyUpdates)
        {
            throw new JavaScriptProxyExecutionFailed($"Observable query did not receive initial update within {timeout.TotalSeconds} seconds");
        }

        // Get all current updates
        var updates = new List<TResult>();
        var updateCount = Runtime.Evaluate<int>("__obsUpdates.length");

        // PATCH: Manually deserialize observable query updates field by field
        // Observable queries don't properly deserialize in test environment due to module isolation
        for (var i = 0; i < updateCount; i++)
        {
            // Apply the same field-by-field deserialization patch for observable query data
            Runtime.Execute($@"
                try {{
                    var update = __obsUpdates[{i}];
                    if (update.data && __obsQuery.modelType && globalThis.Fields) {{
                        var fields = globalThis.Fields.getFieldsForType(__obsQuery.modelType);
                        var deserialized = Array.isArray(update.data) ? [] : {{}};
                        
                        if (Array.isArray(update.data)) {{
                            for (var idx = 0; idx < update.data.length; idx++) {{
                                var item = update.data[idx];
                                var deserializedItem = {{}};
                                for (var i = 0; i < fields.length; i++) {{
                                    var field = fields[i];
                                    if (item.hasOwnProperty(field.name)) {{
                                        deserializedItem[field.name] = item[field.name];
                                    }}
                                }}
                                deserialized.push(deserializedItem);
                            }}
                        }} else {{
                            for (var i = 0; i < fields.length; i++) {{
                                var field = fields[i];
                                if (update.data.hasOwnProperty(field.name)) {{
                                    deserialized[field.name] = update.data[field.name];
                                }}
                            }}
                        }}
                        __obsUpdates[{i}].data = deserialized;
                    }}
                }} catch(e) {{
                    // Silently fail - let original deserialization handle it
                }}
            ");

            var updateJson = Runtime.Evaluate<string>($"JSON.stringify(__obsUpdates[{i}].data)") ?? "{}";

            var update = JsonSerializer.Deserialize<TResult>(updateJson, _jsonOptions);
            if (update is not null)
            {
                updates.Add(update);
            }
        }

        var firstResultJson = Runtime.Evaluate<string>("JSON.stringify(__obsUpdates[0])") ?? "{}";
        var firstResult = JsonSerializer.Deserialize<Queries.QueryResult>(firstResultJson, _jsonOptions);

        return new ObservableQueryExecutionResult<TResult>(
            firstResult,
            updates);
    }

    /// <summary>
    /// Waits for new WebSocket updates to arrive and adds them to the result.
    /// </summary>
    /// <typeparam name="TResult">The type of data.</typeparam>
    /// <param name="result">The execution result to add updates to.</param>
    /// <param name="timeout">Maximum time to wait for updates.</param>
    public async Task WaitForWebSocketUpdates<TResult>(ObservableQueryExecutionResult<TResult> result, TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(5);
        var start = DateTime.UtcNow;
        var initialCount = result.Updates.Count;

        // Wait for new updates to arrive via WebSocket
        while (DateTime.UtcNow - start < timeout)
        {
            var currentCount = Runtime.Evaluate<int>("__obsUpdates.length");
            if (currentCount > initialCount)
            {
                // Get the newly added updates
                for (var i = initialCount; i < currentCount; i++)
                {
                    var updateJson = Runtime.Evaluate<string>($"JSON.stringify(__obsUpdates[{i}].data)") ?? "{}";
                    var update = JsonSerializer.Deserialize<TResult>(updateJson, _jsonOptions);
                    if (update is not null)
                    {
                        result.Updates.Add(update);
                    }
                }
                return;
            }
            await Task.Delay(50);
        }

        throw new JavaScriptProxyExecutionFailed($"No new WebSocket updates received within {timeout.Value.TotalSeconds} seconds");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            // Close all active WebSocket connections
            lock (_webSocketLock)
            {
                foreach (var ws in _webSockets.Values)
                {
                    ws.Dispose();
                }
                _webSockets.Clear();
            }

            Runtime.Dispose();
            _disposed = true;
        }
    }

    void SetupArcGlobals()
    {
        // Set up Arc Globals for origin and API base path
        var uri = new Uri(_serverUrl);
        var origin = $"{uri.Scheme}://{uri.Authority}";

        Runtime.Execute($@"
// Set up Arc Globals by loading and modifying the Globals module
try {{
    var ArcModule = require('@cratis/arc');
    if (ArcModule && ArcModule.Globals) {{
        ArcModule.Globals.origin = '{origin}';
        ArcModule.Globals.apiBasePath = '';
        ArcModule.Globals.microservice = '';
    }}
    
    // Also set on globalThis for compatibility
    if (!globalThis.Globals) {{
        globalThis.Globals = ArcModule.Globals;
    }} else {{
        globalThis.Globals.origin = '{origin}';
        globalThis.Globals.apiBasePath = '';
        globalThis.Globals.microservice = '';
    }}
    
    // CRITICAL: Expose TimeSpan from fundamentals globally for command serialization
    var Fundamentals = require('@cratis/fundamentals');
    if (Fundamentals && Fundamentals.TimeSpan) {{
        globalThis.TimeSpan = Fundamentals.TimeSpan;
    }}
}} catch (e) {{
    console.error('Failed to setup Arc Globals:', e.message);
    throw e;
}}
");
    }

    void SetupWebSocketPolyfill()
    {
        // Expose function to JavaScript that creates a WebSocket connection
        Runtime.Engine.AddHostObject("__createWebSocket", new Func<string, string>((url) =>
        {
            var wsId = CreateWebSocketConnection(url);
            return wsId;
        }));

        // Expose function to send WebSocket messages
        Runtime.Engine.AddHostObject("__webSocketSend", new Action<string, string>((wsId, message) =>
        {
            SendWebSocketMessage(wsId, message);
        }));

        // Expose function to close WebSocket
        Runtime.Engine.AddHostObject("__webSocketClose", new Action<string>((wsId) =>
        {
            CloseWebSocketConnection(wsId);
        }));

        // Set up the WebSocket polyfill
        Runtime.Execute(@"
class WebSocket {
    constructor(url) {
        this.url = url;
        this.readyState = 0; // CONNECTING
        this.onopen = null;
        this.onmessage = null;
        this.onerror = null;
        this.onclose = null;
        this._id = __createWebSocket(url);
        
        // Store reference so C# can call callbacks
        if (!globalThis.__webSockets) {
            globalThis.__webSockets = {};
        }
        globalThis.__webSockets[this._id] = this;
    }
    
    send(data) {
        if (this.readyState !== 1) { // OPEN
            throw new Error('WebSocket is not open');
        }
        __webSocketSend(this._id, typeof data === 'string' ? data : JSON.stringify(data));
    }
    
    close() {
        if (this.readyState === 2 || this.readyState === 3) { // CLOSING or CLOSED
            return;
        }
        this.readyState = 2; // CLOSING
        __webSocketClose(this._id);
    }

}

// WebSocket constants
WebSocket.CONNECTING = 0;
WebSocket.OPEN = 1;
WebSocket.CLOSING = 2;
WebSocket.CLOSED = 3;
");
    }

    string CreateWebSocketConnection(string url)
    {
        var wsId = $"ws_{Interlocked.Increment(ref _nextWebSocketId)}";

        // Convert relative URL to absolute using server URL
        var absoluteUrl = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? url
            : new Uri(new Uri(_serverUrl), url).ToString();

        var connection = new WebSocketConnection(wsId, absoluteUrl, Runtime);

        lock (_webSocketLock)
        {
            _webSockets[wsId] = connection;
        }

        // Start the connection in the background
        _ = connection.ConnectAsync();

        return wsId;
    }

    void SendWebSocketMessage(string wsId, string message)
    {
        lock (_webSocketLock)
        {
            if (_webSockets.TryGetValue(wsId, out var connection))
            {
                _ = connection.SendAsync(message);
            }
        }
    }

    void CloseWebSocketConnection(string wsId)
    {
        lock (_webSocketLock)
        {
            if (_webSockets.TryGetValue(wsId, out var connection))
            {
                connection.Dispose();
                _webSockets.Remove(wsId);
            }
        }
    }

    void SetupFetchInterceptor()
    {
        // Set up the fetch interceptor that stores pending requests
        Runtime.Execute(
            "var __pendingFetch = null;" +
            "function fetch(url, options) {" +
            "    var urlString = (typeof url === 'object' && url.href) ? url.href : String(url);" +
            "    return new Promise(function(resolve, reject) {" +
            "        __pendingFetch = {" +
            "            url: urlString," +
            "            options: options || {}," +
            "            resolve: resolve," +
            "            reject: reject" +
            "        };" +
            "    });" +
            "}");
    }

    async Task<FetchResult> ProcessPendingFetchAsync()
    {
        // Get the pending fetch request details
        var hasPending = Runtime.Evaluate<bool>("__pendingFetch !== null");
        if (!hasPending)
        {
            throw new InvalidOperationException("No pending fetch request found");
        }

        var url = Runtime.Evaluate<string>("__pendingFetch.url") ?? string.Empty;
        var method = Runtime.Evaluate<string>("__pendingFetch.options.method || 'GET'") ?? "GET";
        var bodyJson = Runtime.Evaluate<string>("__pendingFetch.options.body || null");

        // Make the actual HTTP request
        HttpResponseMessage response;
        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            var content = new StringContent(bodyJson ?? "{}", System.Text.Encoding.UTF8, "application/json");
            response = await HttpClient.PostAsync(url, content);
        }
        else
        {
            response = await HttpClient.GetAsync(url);
        }

        var responseContent = await response.Content.ReadAsStringAsync();

        // If response is empty or not successful, provide meaningful error
        if (string.IsNullOrEmpty(responseContent))
        {
            throw new InvalidOperationException($"HTTP {method} to '{url}' returned status {(int)response.StatusCode} with empty response body");
        }

        // Debug: Log the raw response content
        const string responseDebugFile = "/tmp/http-response-content.txt";
        File.WriteAllText(responseDebugFile, $"Response from {url}:\n{responseContent}\n");

        // Resolve the JavaScript promise with the response
        var escapedResponse = responseContent
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");

        Runtime.Execute(
            "if (__pendingFetch) {" +
            "    var responseData = JSON.parse('" + escapedResponse + "');" +
            "    __pendingFetch.resolve({" +
            "        ok: " + (response.IsSuccessStatusCode ? "true" : "false") + "," +
            "        status: " + (int)response.StatusCode + "," +
            "        json: function() { return Promise.resolve(responseData); }," +
            "        text: function() { return Promise.resolve('" + escapedResponse + "'); }" +
            "    });" +
            "    __pendingFetch = null;" +
            "}");

        return new FetchResult(url, method, responseContent);
    }

    record FetchResult(string Url, string Method, string ResponseJson);
}

/// <summary>
/// The exception that is thrown when a JavaScript proxy execution fails.
/// </summary>
/// <param name="message">The error message.</param>
public class JavaScriptProxyExecutionFailed(string message) : Exception(message);

/// <summary>
/// Represents the result of executing a command through the JavaScript proxy.
/// </summary>
/// <typeparam name="TResult">The type of the response data.</typeparam>
/// <param name="Result">The command result.</param>
/// <param name="RawJson">The raw JSON response.</param>
public record CommandExecutionResult<TResult>(Commands.CommandResult<TResult>? Result, string RawJson);

/// <summary>
/// Represents the result of performing a query through the JavaScript proxy.
/// </summary>
/// <typeparam name="TResult">The type of the data.</typeparam>
/// <param name="Result">The query result.</param>
/// <param name="RawJson">The raw JSON response.</param>
/// <param name="RequestUrl">The URL that was requested.</param>
public record QueryExecutionResult<TResult>(Queries.QueryResult? Result, string RawJson, string RequestUrl);

/// <summary>
/// Represents the result of performing an observable query through the JavaScript proxy.
/// </summary>
/// <typeparam name="TResult">The type of the data.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ObservableQueryExecutionResult{TResult}"/> class.
/// </remarks>
/// <param name="result">The initial query result.</param>
/// <param name="updates">All updates received.</param>
public class ObservableQueryExecutionResult<TResult>(Queries.QueryResult result, List<TResult> updates)
{
    /// <summary>
    /// Gets the initial query result.
    /// </summary>
    public Queries.QueryResult Result { get; } = result;

    /// <summary>
    /// Gets all updates received.
    /// </summary>
    public List<TResult> Updates { get; } = updates;

    /// <summary>
    /// Gets the most recent data from updates.
    /// </summary>
    public TResult LatestData => Updates[^1];
}

/// <summary>
/// Represents a WebSocket connection bridged between JavaScript and .NET.
/// </summary>
sealed class WebSocketConnection : IDisposable
{
    readonly string _id;
    readonly string _url;
    readonly JavaScriptRuntime _runtime;
    readonly CancellationTokenSource _cts = new();
    ClientWebSocket? _webSocket;
    bool _disposed;

    public WebSocketConnection(string id, string url, JavaScriptRuntime runtime)
    {
        _id = id;
        _url = url;
        _runtime = runtime;
    }

    public async Task ConnectAsync()
    {
        var logPath = "/tmp/websocket-debug.txt";
        try
        {
            _webSocket = new ClientWebSocket();

            // Convert http/https URL to ws/wss
            var uri = new Uri(_url);
            var wsUri = new UriBuilder(uri)
            {
                Scheme = uri.Scheme == "https" ? "wss" : "ws"
            }.Uri;

            // Set the Origin header to match the server URL, as browsers do
            // This is often required by ASP.NET Core WebSocket middleware for security
            var serverUri = new Uri(_runtime.Evaluate<string>("globalThis.Globals.origin") ?? _url);
            var origin = $"{serverUri.Scheme}://{serverUri.Authority}";
            _webSocket.Options.SetRequestHeader("Origin", origin);

            File.AppendAllText(logPath, $"[WebSocket {_id}] Connecting to: {wsUri}\n");
            File.AppendAllText(logPath, $"[WebSocket {_id}] Origin: {origin}\n");

            // Connect to the real Kestrel server
            await _webSocket.ConnectAsync(wsUri, _cts.Token);

            File.AppendAllText(logPath, $"[WebSocket {_id}] Connected successfully\n");

            // Notify JavaScript that connection is open
            _runtime.Execute($@"
if (globalThis.__webSockets && globalThis.__webSockets['{_id}']) {{
    var ws = globalThis.__webSockets['{_id}'];
    ws.readyState = 1; // OPEN
    if (ws.onopen) ws.onopen({{ type: 'open' }});
}}");

            // Start receiving messages
            _ = Task.Run(async () => await ReceiveLoop());
        }
        catch (Exception ex)
        {
            File.AppendAllText(logPath, $"[WebSocket {_id}] Connection failed: {ex.Message}\n");
            File.AppendAllText(logPath, $"[WebSocket {_id}] Stack trace: {ex.StackTrace}\n");

            // Notify JavaScript of error
            var errorMsg = ex.Message.Replace("'", "\\'").Replace("\n", "\\n");
            _runtime.Execute($@"
if (globalThis.__webSockets && globalThis.__webSockets['{_id}']) {{
    var ws = globalThis.__webSockets['{_id}'];
    ws.readyState = 3; // CLOSED
    if (ws.onerror) ws.onerror({{ type: 'error', message: '{errorMsg}' }});
    if (ws.onclose) ws.onclose({{ type: 'close', code: 1006, reason: '{errorMsg}' }});
}}");
        }
    }

    public async Task SendAsync(string message)
    {
        if (_webSocket?.State == WebSocketState.Open)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cts.Token);
        }
    }

    async Task ReceiveLoop()
    {
        if (_webSocket is null) return;

        var logPath = "/tmp/websocket-debug.txt";
        File.AppendAllText(logPath, $"[WebSocket {_id}] ReceiveLoop started\n");

        var buffer = new byte[1024 * 4];
        try
        {
            while (_webSocket.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
            {
                File.AppendAllText(logPath, $"[WebSocket {_id}] Waiting for message...\n");
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                File.AppendAllText(logPath, $"[WebSocket {_id}] Received message type: {result.MessageType}, count: {result.Count}\n");

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);

                    _runtime.Execute($@"
if (globalThis.__webSockets && globalThis.__webSockets['{_id}']) {{
    var ws = globalThis.__webSockets['{_id}'];
    ws.readyState = 3; // CLOSED
    if (ws.onclose) ws.onclose({{ type: 'close', code: 1000, reason: 'Normal closure' }});
}}");
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                File.AppendAllText(logPath, $"[WebSocket {_id}] Message content: {message.Substring(0, Math.Min(200, message.Length))}...\n");

                File.AppendAllText(logPath, $"[WebSocket {_id}] About to invoke JS onmessage via host\n");

                try
                {
                    // Ensure we pass a plain JavaScript string as the event data.
                    // Escape single quotes, backslashes and newlines so the literal is safe when injected.
                    var escaped = message
                        .Replace("\\", "\\\\")
                        .Replace("'", "\\'")
                        .Replace("\r", "\\r")
                        .Replace("\n", "\\n");

                    var injectedSnippet = $@"if (globalThis.__webSockets && globalThis.__webSockets['{_id}']) {{
    var ws = globalThis.__webSockets['{_id}'];
    if (ws.onmessage) ws.onmessage({{ type: 'message', data: '{escaped}' }});
}}";

                    // Log the exact JS snippet we will inject so we can correlate host -> injected -> parsed
                    try
                    {
                        File.AppendAllText(logPath, $"[WebSocket {_id}] Injected JS snippet:\n{injectedSnippet}\n");
                    }
                    catch { }

                    _runtime.Execute(injectedSnippet);

                    File.AppendAllText(logPath, $"[WebSocket {_id}] JavaScript onmessage invoked via host\n");
                }
                catch (Exception ex)
                {
                    File.AppendAllText(logPath, $"[WebSocket {_id}] Error invoking onmessage via host: {ex.Message}\n");
                }
                File.AppendAllText(logPath, $"[WebSocket {_id}] JavaScript onmessage called\n");
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            var errorMsg = ex.Message.Replace("'", "\\'").Replace("\n", "\\n");
            _runtime.Execute($@"
if (globalThis.__webSockets && globalThis.__webSockets['{_id}']) {{
    var ws = globalThis.__webSockets['{_id}'];
    ws.readyState = 3; // CLOSED
    if (ws.onerror) ws.onerror({{ type: 'error', message: '{errorMsg}' }});
    if (ws.onclose) ws.onclose({{ type: 'close', code: 1006, reason: '{errorMsg}' }});
}}");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cts.Cancel();
            _webSocket?.Dispose();
            _cts.Dispose();
            _disposed = true;
        }
    }
}
