// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// Manages Service Broker enablement for SQL Server databases.
/// </summary>
public interface IServiceBrokerManager
{
    /// <summary>
    /// Ensures Service Broker is enabled for the database, attempting to enable it if necessary.
    /// </summary>
    /// <param name="connectionString">The connection string to the database.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EnsureEnabled(string connectionString, CancellationToken cancellationToken = default);
}
