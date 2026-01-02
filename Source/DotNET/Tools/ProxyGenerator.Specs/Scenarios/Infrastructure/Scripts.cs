// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Provides JavaScript scripts for test infrastructure.
/// </summary>
public static class Scripts
{
    static readonly string _executeCommand;
    static readonly string _patchCommandTimeSpan;
    static readonly string _subscribeObservableQuery;
    static readonly string _deserializeQueryResult;
    static readonly string _setupArcGlobals;

    static Scripts()
    {
        // Get the directory containing the compiled assembly
        var assemblyDir = Path.GetDirectoryName(typeof(Scripts).Assembly.Location)!;

        // Try to find the Scripts directory relative to the assembly location
        // In debug builds: bin/Debug/net10.0 -> go up 3 levels, then to Scenarios/Infrastructure/Scripts
        var scriptsPath = Path.Combine(assemblyDir, "..", "..", "..", "Scenarios", "Infrastructure", "Scripts");
        scriptsPath = Path.GetFullPath(scriptsPath);

        // Verify the path exists
        if (!Directory.Exists(scriptsPath))
        {
            throw new DirectoryNotFoundException($"Scripts directory not found at: {scriptsPath}");
        }

        _executeCommand = File.ReadAllText(Path.Combine(scriptsPath, "execute-command.js"));
        _patchCommandTimeSpan = File.ReadAllText(Path.Combine(scriptsPath, "patch-command-timespan.js"));
        _subscribeObservableQuery = File.ReadAllText(Path.Combine(scriptsPath, "subscribe-observable-query.js"));
        _deserializeQueryResult = File.ReadAllText(Path.Combine(scriptsPath, "deserialize-query-result.js"));
        _setupArcGlobals = File.ReadAllText(Path.Combine(scriptsPath, "setup-arc-globals.js"));
        WebSocketPolyfill = File.ReadAllText(Path.Combine(scriptsPath, "websocket-polyfill.js"));
        FetchInterceptor = File.ReadAllText(Path.Combine(scriptsPath, "fetch-interceptor.js"));
    }

    /// <summary>
    /// Gets the script for executing a command.
    /// </summary>
    /// <param name="commandClassName">The command class name.</param>
    /// <param name="propertyAssignments">The property assignments.</param>
    /// <returns>The formatted script.</returns>
    public static string ExecuteCommand(string commandClassName, string propertyAssignments) =>
        _executeCommand
            .Replace("{{COMMAND_CLASS}}", commandClassName)
            .Replace("{{PROPERTY_ASSIGNMENTS}}", propertyAssignments);

    /// <summary>
    /// Gets the script for patching TimeSpan values in command results.
    /// </summary>
    /// <param name="escapedResponse">The escaped HTTP response JSON.</param>
    /// <returns>The formatted script.</returns>
    public static string PatchCommandTimeSpan(string escapedResponse) =>
        _patchCommandTimeSpan.Replace("{{ESCAPED_RESPONSE}}", escapedResponse);

    /// <summary>
    /// Gets the script for subscribing to an observable query.
    /// </summary>
    /// <param name="queryClassName">The query class name.</param>
    /// <param name="parameterAssignments">The parameter assignments.</param>
    /// <param name="subscriptionId">The unique subscription ID.</param>
    /// <returns>The formatted script.</returns>
    public static string SubscribeObservableQuery(string queryClassName, string parameterAssignments, string subscriptionId) =>
        _subscribeObservableQuery
            .Replace("{{QUERY_CLASS}}", queryClassName)
            .Replace("{{PARAMETER_ASSIGNMENTS}}", parameterAssignments)
            .Replace("{{SUBSCRIPTION_ID}}", subscriptionId);

    /// <summary>
    /// Gets the script for deserializing query results.
    /// </summary>
    /// <param name="escapedJson">The escaped response JSON.</param>
    /// <returns>The formatted script.</returns>
    public static string DeserializeQueryResult(string escapedJson) =>
        _deserializeQueryResult.Replace("{{ESCAPED_JSON}}", escapedJson);

    /// <summary>
    /// Gets the script for setting up Arc globals.
    /// </summary>
    /// <param name="origin">The origin URL.</param>
    /// <returns>The formatted script.</returns>
    public static string SetupArcGlobals(string origin) =>
        _setupArcGlobals.Replace("{{ORIGIN}}", origin);

    /// <summary>
    /// Gets the WebSocket polyfill script.
    /// </summary>
    public static string WebSocketPolyfill { get; }

    /// <summary>
    /// Gets the fetch interceptor script.
    /// </summary>
    public static string FetchInterceptor { get; }
}
