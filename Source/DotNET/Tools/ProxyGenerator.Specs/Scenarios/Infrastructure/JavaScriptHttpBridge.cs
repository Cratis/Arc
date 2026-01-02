// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

#pragma warning disable MA0101 // String contains an implicit end of line character

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
        Runtime.Execute(Scripts.ExecuteCommand(commandClassName, propAssignments));

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
            Runtime.Execute(Scripts.PatchCommandTimeSpan(escapedResponse));
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
        var argsObject = parameters?.Count > 0
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

        Runtime.Execute(queryScript);

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
                requestUrl = route + (queryParams?.Any() == true ? "?" + string.Join('&', queryParams) : "");
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
            // Escape the response JSON for embedding in JavaScript string
            var escapedJson = result.ResponseJson.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r");
            Runtime.Execute(Scripts.DeserializeQueryResult(escapedJson));
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
        Runtime.Execute(Scripts.SubscribeObservableQuery(queryClassName, paramAssignments));

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
            await Task.Delay(10);
        }

        var hasAnyUpdates = Runtime.Evaluate<bool>("__obsUpdates.length > 0");
        if (!hasAnyUpdates)
        {
            throw new JavaScriptProxyExecutionFailed($"Observable query did not receive initial update within {timeout.TotalSeconds} seconds");
        }

        // Get all current updates - already deserialized by TypeScript JsonSerializer
        var updates = new List<TResult>();
        var updateCount = Runtime.Evaluate<int>("__obsUpdates.length");

        // Extract already-deserialized data from TypeScript (stringify the deserialized objects)
        for (var i = 0; i < updateCount; i++)
        {
            // TypeScript has already called JsonSerializer.deserializeArrayFromInstance/deserializeFromInstance
            // so __obsUpdates[i].data contains properly typed objects. We just need to extract them to C#.
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
    /// <returns>An awaitable task.</returns>
    /// <exception cref="JavaScriptProxyExecutionFailed">The exception that is thrown when no updates are received within the timeout.</exception>
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
                // Get the newly added updates - already deserialized by TypeScript JsonSerializer
                for (var i = initialCount; i < currentCount; i++)
                {
                    // TypeScript has already called JsonSerializer.deserializeArrayFromInstance/deserializeFromInstance
                    var updateJson = Runtime.Evaluate<string>($"JSON.stringify(__obsUpdates[{i}].data)") ?? "{}";
                    var update = JsonSerializer.Deserialize<TResult>(updateJson, _jsonOptions);
                    if (update is not null)
                    {
                        result.Updates.Add(update);
                    }
                }
                return;
            }
            await Task.Delay(10);
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

        Runtime.Execute(Scripts.SetupArcGlobals(origin));
    }

    void SetupWebSocketPolyfill()
    {
        // Expose function to JavaScript that creates a WebSocket connection
        Runtime.Engine.AddHostObject("__createWebSocket", new Func<string, string>(CreateWebSocketConnection));

        // Expose function to send WebSocket messages
        Runtime.Engine.AddHostObject("__webSocketSend", new Action<string, string>(SendWebSocketMessage));

        // Expose function to close WebSocket
        Runtime.Engine.AddHostObject("__webSocketClose", new Action<string>(CloseWebSocketConnection));

        // Set up the WebSocket polyfill
        Runtime.Execute(Scripts.WebSocketPolyfill);
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
        Runtime.Execute(Scripts.FetchInterceptor);
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
            var content = new StringContent(bodyJson ?? "{}", Encoding.UTF8, "application/json");
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
