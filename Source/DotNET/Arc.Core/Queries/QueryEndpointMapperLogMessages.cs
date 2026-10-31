// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Log messages for <see cref="QueryEndpointMapper"/>.
/// </summary>
internal static partial class QueryEndpointMapperLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Query {SkippedQuery} is not mapped because its route {Route} is already used by query {MappedQuery}. Give one of them a different name or a [Route] to make it reachable")]
    internal static partial void PublicQueryRouteCollision(this ILogger logger, FullyQualifiedQueryName skippedQuery, string route, FullyQualifiedQueryName mappedQuery);

    [LoggerMessage(LogLevel.Debug, "Internal query {SkippedQuery} is not mapped because its route {Route} is already used by query {MappedQuery}")]
    internal static partial void InternalQueryRouteCollision(this ILogger logger, FullyQualifiedQueryName skippedQuery, string route, FullyQualifiedQueryName mappedQuery);
}
