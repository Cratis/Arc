// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Http;

/// <summary>
/// Log messages for <see cref="WellKnownRoutesMiddleware"/>.
/// </summary>
internal static partial class WellKnownRoutesMiddlewareLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Registering route {Method} {Pattern}")]
    internal static partial void RegisteringRoute(this ILogger<WellKnownRoutesMiddleware> logger, string method, string pattern);

    [LoggerMessage(LogLevel.Debug, "Checking for route match: {RouteKey}")]
    internal static partial void CheckingRouteMatch(this ILogger<WellKnownRoutesMiddleware> logger, string routeKey);

    [LoggerMessage(LogLevel.Debug, "Route matched: {RouteKey}")]
    internal static partial void RouteMatched(this ILogger<WellKnownRoutesMiddleware> logger, string routeKey);
}
