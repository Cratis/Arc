// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// Interceptor that detects changes after SaveChanges and notifies subscribers.
/// </summary>
/// <param name="changeTracker">The <see cref="IEntityChangeTracker"/> to notify.</param>
/// <param name="logger">The <see cref="ILogger"/> for diagnostics.</param>
public sealed class ObserveInterceptor(IEntityChangeTracker changeTracker, ILogger<ObserveInterceptor> logger) : SaveChangesInterceptor
{
    HashSet<string> _changedTables = [];

    /// <inheritdoc/>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        CaptureChangedTables(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        CaptureChangedTables(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        NotifyChanges();
        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        NotifyChanges();
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    void CaptureChangedTables(DbContext? context)
    {
        if (context is null)
        {
            logger.CaptureChangedTablesNullContext();
            return;
        }

        // Capture table names before SaveChanges completes.
        // Using table names instead of CLR types avoids issues with EF Core proxy classes
        // and ensures notifications match the registered observers.
        // This is important because deleted entities are detached after SaveChanges.
        _changedTables = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .Select(e =>
            {
                var tblName = e.Metadata.GetTableName();
                var schema = e.Metadata.GetSchema() ?? e.Metadata.Model.GetDefaultSchema();
                return schema is not null ? $"{schema}.{tblName}" : tblName;
            })
            .Where(tableName => tableName is not null)
            .Cast<string>()
            .ToHashSet();

        logger.CapturedChangedTables(_changedTables.Count, string.Join(", ", _changedTables));
    }

    void NotifyChanges()
    {
        logger.NotifyingChanges(_changedTables.Count, string.Join(", ", _changedTables));

        foreach (var tableName in _changedTables)
        {
            logger.NotifyingChangeForTable(tableName);
            changeTracker.NotifyChange(tableName);
        }

        _changedTables.Clear();
    }
}
