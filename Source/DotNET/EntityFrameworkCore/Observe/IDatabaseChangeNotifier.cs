// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// Defines a service that provides database-level change notifications.
/// </summary>
public interface IDatabaseChangeNotifier : IAsyncDisposable
{
    /// <summary>
    /// Starts listening for changes on the specified table.
    /// </summary>
    /// <param name="tableName">The name of the table to listen for changes on.</param>
    /// <param name="schemaName">The optional schema name of the table. When <see langword="null"/>, the database default schema is used.</param>
    /// <param name="columnNames">The column names to monitor for changes.</param>
    /// <param name="onChanged">The callback to invoke when changes are detected.</param>
    /// <param name="cancellationToken">A cancellation token to stop listening.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartListening(string tableName, string? schemaName, IEnumerable<string> columnNames, Action onChanged, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops listening for changes.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopListening();
}
