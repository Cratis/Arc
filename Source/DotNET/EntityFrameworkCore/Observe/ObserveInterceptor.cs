// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// Interceptor that detects changes after SaveChanges and notifies subscribers.
/// </summary>
/// <param name="changeTracker">The <see cref="IEntityChangeTracker"/> to notify.</param>
public sealed class ObserveInterceptor(IEntityChangeTracker changeTracker) : SaveChangesInterceptor
{
    /// <inheritdoc/>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        CaptureChangedEntityTypes(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        CaptureChangedEntityTypes(eventData.Context);
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

    HashSet<Type> _changedEntityTypes = [];

    void CaptureChangedEntityTypes(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        // Capture entity types before SaveChanges completes
        // This is important because deleted entities are detached after SaveChanges
        _changedEntityTypes = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .Select(e => e.Entity.GetType())
            .ToHashSet();
    }

    void NotifyChanges()
    {
        foreach (var entityType in _changedEntityTypes)
        {
            changeTracker.NotifyChange(entityType);
        }

        _changedEntityTypes.Clear();
    }
}
