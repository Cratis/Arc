// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Http;

/// <summary>
/// Holds all log messages for <see cref="HttpListenerEndpointMapper"/>.
/// </summary>
internal static partial class HttpListenerEndpointMapperLogMessages
{
    [LoggerMessage(LogLevel.Information, "HttpListener started on: {Prefixes}")]
    internal static partial void HttpListenerStarted(this ILogger<HttpListenerEndpointMapper> logger, string prefixes);

    [LoggerMessage(LogLevel.Error, "Error in HTTP listener loop")]
    internal static partial void ErrorInListenerLoop(this ILogger<HttpListenerEndpointMapper> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Error handling request")]
    internal static partial void ErrorHandlingRequest(this ILogger<HttpListenerEndpointMapper> logger, Exception exception);
}
