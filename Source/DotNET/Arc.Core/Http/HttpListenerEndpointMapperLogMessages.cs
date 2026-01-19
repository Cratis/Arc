// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Http;

/// <summary>
/// Holds all log messages for <see cref="HttpListenerEndpointMapper"/>.
/// </summary>
internal static partial class HttpListenerEndpointMapperLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Adding {Count} HTTP listener prefixes")]
    internal static partial void AddingHttpListenerPrefixes(this ILogger<HttpListenerEndpointMapper> logger, int count);

    [LoggerMessage(LogLevel.Debug, "Adding HTTP listener prefix: {Prefix}")]
    internal static partial void AddingHttpListenerPrefix(this ILogger<HttpListenerEndpointMapper> logger, string prefix);

    [LoggerMessage(LogLevel.Information, "HttpListener started on: {Prefixes}")]
    internal static partial void HttpListenerStarted(this ILogger<HttpListenerEndpointMapper> logger, string prefixes);

    [LoggerMessage(LogLevel.Error, "Error in HTTP listener loop")]
    internal static partial void ErrorInListenerLoop(this ILogger<HttpListenerEndpointMapper> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Error handling request")]
    internal static partial void ErrorHandlingRequest(this ILogger<HttpListenerEndpointMapper> logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Registering route: {Method} {Pattern}")]
    internal static partial void RegisteringRoute(this ILogger<HttpListenerEndpointMapper> logger, string method, string pattern);

    [LoggerMessage(LogLevel.Debug, "Incoming request: {Method} {Path} (WebSocket: {IsWebSocket})")]
    internal static partial void IncomingRequest(this ILogger<HttpListenerEndpointMapper> logger, string method, string path, bool isWebSocket);

    [LoggerMessage(LogLevel.Debug, "Route matched: {RouteKey}")]
    internal static partial void RouteMatched(this ILogger<HttpListenerEndpointMapper> logger, string routeKey);

    [LoggerMessage(LogLevel.Debug, "Route not found: {RouteKey}. Available routes: {AvailableRoutes}")]
    internal static partial void RouteNotFound(this ILogger<HttpListenerEndpointMapper> logger, string routeKey, string availableRoutes);
}
