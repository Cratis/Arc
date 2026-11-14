// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Neo4j.Chronicle;

/// <summary>
/// Holds the log messages of <see cref="Neo4jGraphQueryExecutor"/>.
/// </summary>
internal static partial class Neo4jGraphQueryExecutorLogging
{
    [LoggerMessage(LogLevel.Debug, "Reading the view '{View}' of the graph '{Graph}' from the database '{Database}'")]
    internal static partial void ExecutingGraphQuery(this ILogger<Neo4jGraphQueryExecutor> logger, string graph, string view, string database);
}
