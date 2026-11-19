// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Neo4j.Chronicle;

/// <summary>
/// Holds the log messages of <see cref="GraphViewsDiscovery"/>.
/// </summary>
internal static partial class GraphViewsDiscoveryLogging
{
    [LoggerMessage(LogLevel.Warning, "The graph view '{View}' was left out and cannot be queried: {Reason}")]
    internal static partial void ViewLeftOut(this ILogger<GraphViewsDiscovery> logger, string view, string reason);
}
