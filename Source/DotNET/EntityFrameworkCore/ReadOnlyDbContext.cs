// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Represents a DbContext that does not allow saving changes.
/// </summary>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
public class ReadOnlyDbContext(DbContextOptions options) : BaseDbContext(options)
{
    /// <summary>
    /// Gets a value indicating whether eager loading is enabled for all navigations.
    /// </summary>
    protected virtual bool IsEagerLoadingEnabled => true;

    /// <inheritdoc/>
    public override int SaveChanges() => throw new InvalidOperationException("This DbContext is read-only and does not support saving changes.");

    /// <inheritdoc/>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException("This DbContext is read-only and does not support saving changes.");

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        if (!IsEagerLoadingEnabled)
        {
            return;
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var navigation in entityType.GetNavigations())
            {
                navigation.SetIsEagerLoaded(true);
            }
        }
    }
}
