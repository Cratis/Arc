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
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        NotifyChanges(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        NotifyChanges(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    void NotifyChanges(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        // Get all entity types that have been tracked for changes
        // After SaveChanges, entries are marked as Unchanged, so we need to get all unique types
        var changedEntityTypes = context.ChangeTracker.Entries()
            .Select(e => e.Entity.GetType())
            .Distinct()
            .ToList();

        foreach (var entityType in changedEntityTypes)
        {
            changeTracker.NotifyChange(entityType);
        }
    }
}
