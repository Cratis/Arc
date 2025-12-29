// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Bridges the JavaScript runtime with an HTTP client for testing proxies end-to-end.
/// Intercepts fetch calls from JavaScript and routes them to the test HTTP client.
/// </summary>
public sealed class JavaScriptHttpBridge : IDisposable
{
    readonly JsonSerializerOptions _jsonOptions;
    bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="JavaScriptHttpBridge"/> class.
    /// </summary>
    /// <param name="runtime">The JavaScript runtime.</param>
    /// <param name="httpClient">The HTTP client to use for requests.</param>
    public JavaScriptHttpBridge(JavaScriptRuntime runtime, HttpClient httpClient)
    {
        Runtime = runtime;
        HttpClient = httpClient;
        _jsonOptions = Json.Globals.JsonSerializerOptions;
        SetupFetchInterceptor();
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

        // DEBUG: Save ALL transpiled code for inspection
        var debugDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "arc-debug");
        System.IO.Directory.CreateDirectory(debugDir);

        var timestamp = DateTime.Now.ToString("HHmmss-fff");
        var tsPath = System.IO.Path.Combine(debugDir, $"code_{timestamp}.ts");
        System.IO.File.WriteAllText(tsPath, typeScriptCode);

        var jsPath = System.IO.Path.Combine(debugDir, $"code_{timestamp}.js");
        System.IO.File.WriteAllText(jsPath, jsCode);

        if (wrapInModuleScope)
        {
            // Wrap in module scope to prevent variable collisions when multiple files
            // import from the same modules (e.g., @cratis/fundamentals), but still export
            // classes to global scope so they're accessible
#pragma warning disable MA0136 // Raw String contains an implicit end of line character
            var wrappedCode = $$"""
(function() {
    var module = { exports: {} };
    var exports = module.exports;
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
            properties[prop.Name] = prop.Value.Deserialize<object>(_jsonOptions)!;
        }

        // Set up properties on the command
        var propAssignments = string.Concat(properties.Select(p =>
            $"__cmd.{p.Key} = {JsonSerializer.Serialize(p.Value, _jsonOptions)};"));

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
        // Set up parameters on the query
        var paramAssignments = parameters is not null
            ? string.Concat(parameters.Select(p => $"__query.{p.Key} = {JsonSerializer.Serialize(p.Value, _jsonOptions)};"))
            : string.Empty;

        // Create query instance and set parameters, then call perform()
        Runtime.Execute(
            "var __query = new " + queryClassName + "();" +
            paramAssignments +
            "var __queryResult = null;" +
            "var __queryError = null;" +
            "var __queryDone = false;" +
            "__query.perform().then(function(result) {" +
            "    __queryResult = result;" +
            "    __queryDone = true;" +
            "}).catch(function(error) {" +
            "    __queryError = error;" +
            "    __queryDone = true;" +
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
        SpinWait.SpinUntil(() => (bool)Runtime.Evaluate("__queryDone")!, TimeSpan.FromSeconds(5));

        var hasError = Runtime.Evaluate<bool>("__queryError !== null");
        if (hasError)
        {
            var errorMsg = Runtime.Evaluate<string>("__queryError?.message || String(__queryError)");
            throw new JavaScriptProxyExecutionFailed($"Query execution failed: {errorMsg}");
        }

        // Get the result from JavaScript
        var resultJson = Runtime.Evaluate<string>("JSON.stringify(__queryResult)") ?? "{}";
        var queryResult = JsonSerializer.Deserialize<Queries.QueryResult>(resultJson, _jsonOptions);

        return new QueryExecutionResult<TResult>(queryResult, result?.ResponseJson, result?.Url);
    }

    /// <summary>
    /// Performs an observable query through its JavaScript proxy class.
    /// The proxy's subscribe() method simulates a WebSocket connection for real-time updates.
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

        // Add debug logging function
        Runtime.Execute("var __fetchResponses = []; var __logFetchResponse = function(url, data) { __fetchResponses.push({url: url, data: data}); };");

        // Create query instance, set parameters, and subscribe
        Runtime.Execute(
            "var __obsQuery = new " + queryClassName + "();" +
            paramAssignments +
            "var __obsUpdates = [];" +
            "var __obsError = null;" +
            "var __obsSubscription = __obsQuery.subscribe(function(result) {" +
            "    __obsUpdates.push(result);" +
            "});");

        // Process the pending fetch from subscribe
        var hasPendingFetch = Runtime.Evaluate<bool>("__pendingFetch !== null");
        if (!hasPendingFetch)
        {
            throw new JavaScriptProxyExecutionFailed("Observable query subscribe did not initiate a fetch");
        }

        // Process the fetch to get the initial data
        await ProcessPendingFetchAsync();

        // Give the promise time to resolve
        await Task.Delay(10);

        // Log what fetch actually returned
        var fetchResponseCount = Runtime.Evaluate<int>("__fetchResponses.length");
        if (fetchResponseCount > 0)
        {
            var fetchResponseJson = Runtime.Evaluate<string>("JSON.stringify(__fetchResponses[0])") ?? "{}";
            System.IO.File.WriteAllText("/tmp/fetch-response.json", fetchResponseJson);
        }

        // Get the initial result
        var hasUpdates = Runtime.Evaluate<bool>("__obsUpdates.length > 0");
        if (!hasUpdates)
        {
            throw new JavaScriptProxyExecutionFailed("Observable query did not produce any initial result");
        }

        var updates = new List<TResult>();
        var updateCount = Runtime.Evaluate<int>("__obsUpdates.length");

        for (var i = 0; i < updateCount; i++)
        {
            var fullUpdateJson = Runtime.Evaluate<string>($"JSON.stringify(__obsUpdates[{i}])") ?? "{}";
            System.IO.File.WriteAllText($"/tmp/obs-update-{i}.json", fullUpdateJson);
            
            var updateJson = Runtime.Evaluate<string>($"JSON.stringify(__obsUpdates[{i}].data)") ?? "{}";
            System.IO.File.WriteAllText($"/tmp/obs-data-{i}.json", updateJson);
            
            var update = JsonSerializer.Deserialize<TResult>(updateJson, _jsonOptions);
            if (update is not null)
            {
                updates.Add(update);
            }
        }

        var firstResultJson = Runtime.Evaluate<string>("JSON.stringify(__obsUpdates[0])") ?? "{}";
        var firstResult = JsonSerializer.Deserialize<Queries.QueryResult>(firstResultJson, _jsonOptions);

        return new ObservableQueryExecutionResult<TResult>(
            firstResult!,
            updates,
            updates[^1]);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            Runtime.Dispose();
            _disposed = true;
        }
    }

    void SetupFetchInterceptor()
    {
        // Set up the fetch interceptor that stores pending requests
        Runtime.Execute(
            "var __pendingFetch = null;" +
            "function fetch(url, options) {" +
            "    return new Promise(function(resolve, reject) {" +
            "        __pendingFetch = {" +
            "            url: url," +
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
/// <param name="Result">The initial query result.</param>
/// <param name="Updates">All updates received.</param>
/// <param name="LatestData">The most recent data from updates.</param>
public record ObservableQueryExecutionResult<TResult>(Queries.QueryResult Result, List<TResult> Updates, TResult LatestData);
