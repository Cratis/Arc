// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Logging for collection subscriptions without item identity.
/// </summary>
internal static partial class ObservableQueryDemultiplexerLogging
{
    [LoggerMessage(LogLevel.Warning, "Observable query '{QueryName}' (subscription '{QueryId}') sends full collection snapshots without change sets because its items have no Id property; delta transfer requires a stable identity (Id) on each item")]
    internal static partial void CollectionWithoutIdentity(this ILogger<ObservableQueryDemultiplexer> logger, string queryId, string queryName);
}
