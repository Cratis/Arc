// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Holds all log messages for <see cref="Neo4jDriverFactory"/>.
/// </summary>
internal static partial class Neo4jDriverFactoryLogMessages
{
    [LoggerMessage(LogLevel.Trace, "Creating Neo4j driver for connecting to '{Address}'")]
    internal static partial void CreateDriver(this ILogger<Neo4jDriverFactory> logger, string address);
}
