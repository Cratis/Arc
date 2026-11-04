// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Metrics;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Dotted MongoDB client instruments, emitted alongside the legacy instruments.
/// </summary>
internal static partial class AlignedMongoDBClientMetrics
{
    /// <summary>Records the open connection count.</summary>
    /// <param name="meter">The client meter scope.</param>
    /// <param name="count">The current count.</param>
    [Gauge<int>("mongodb.open.connections", "Number of open connections")]
    internal static partial void OpenConnections(this IMeterScope<MongoDBClientFactory> meter, int count);

    /// <summary>Records the checked out connection count.</summary>
    /// <param name="meter">The client meter scope.</param>
    /// <param name="count">The current count.</param>
    [Gauge<int>("mongodb.checked.out.connections", "Number of connections checked out from the pool")]
    internal static partial void CheckedOutConnections(this IMeterScope<MongoDBClientFactory> meter, int count);

    /// <summary>Records the pooled connection count.</summary>
    /// <param name="meter">The client meter scope.</param>
    /// <param name="count">The current count.</param>
    [Gauge<int>("mongodb.connections.in.pool", "Number of connections added to the pool")]
    internal static partial void ConnectionsAddedToPool(this IMeterScope<MongoDBClientFactory> meter, int count);

    /// <summary>Records the active command count.</summary>
    /// <param name="meter">The client meter scope.</param>
    /// <param name="count">The current count.</param>
    [Gauge<int>("mongodb.commands", "Number of commands")]
    internal static partial void Commands(this IMeterScope<MongoDBClientFactory> meter, int count);

    /// <summary>Records a failed connection.</summary>
    /// <param name="meter">The client meter scope.</param>
    [Counter<int>("mongodb.failed.connections", "Number of failed connections")]
    internal static partial void FailedConnections(this IMeterScope<MongoDBClientFactory> meter);

    /// <summary>Records a command start.</summary>
    /// <param name="meter">The client meter scope.</param>
    [Counter<int>("mongodb.aggregated.commands", "Number of aggregated commands")]
    internal static partial void AggregatedCommands(this IMeterScope<MongoDBClientFactory> meter);
}
