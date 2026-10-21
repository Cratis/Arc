// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.EntityFrameworkCore;

namespace Microsoft.EntityFrameworkCore;

/// <summary>
/// Extensions for working with DbContexts.
/// </summary>
public static class DbSetExtensions
{
    /// <summary>
    /// Upserts an item in a DbSet.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <param name="dbSet">The DbSet to upsert the item in.</param>
    /// <param name="item">The item to upsert.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// An upsert will add the item if it does not exist, or update the existing item if it does. The existence is determined based on the primary key(s) defined for the entity.
    /// This will potentially lead to two database calls - one to check for existence and another to insert or update.
    /// It will check the local tracked entities first before querying the database.
    /// </remarks>
    /// <exception cref="EntityDoesNotHavePrimaryKey">Thrown if the entity does not have a primary key.</exception>
    public static Task Upsert<[DynamicallyAccessedMembers(EntityTypeMembers.Required)] TEntity>(this DbSet<TEntity> dbSet, TEntity item)
        where TEntity : class => dbSet.UpsertTracked(item, CancellationToken.None);

    /// <summary>
    /// Upserts an item atomically using the database provider's native SQL when supported.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <param name="dbSet">The DbSet to upsert the item in.</param>
    /// <param name="item">The item to upsert.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// SQL Server, PostgreSQL, and SQLite execute immediately, bypassing change tracking. Tracked instances
    /// and generated values are not refreshed. Other providers and mappings requiring tracked persistence
    /// (including generated keys, navigations, shadow properties, owned entities, inheritance, and multiple tables) fall back to
    /// <see cref="Upsert{TEntity}"/> and require SaveChanges. Use an application-assigned key for atomic upserts.
    /// </remarks>
    /// <exception cref="EntityDoesNotHavePrimaryKey">Thrown if the entity does not have a primary key.</exception>
    public static async Task UpsertAsync<[DynamicallyAccessedMembers(EntityTypeMembers.Required)] TEntity>(this DbSet<TEntity> dbSet, TEntity item, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!await NativeUpsert.TryExecute(dbSet, item, cancellationToken))
        {
            await dbSet.UpsertTracked(item, cancellationToken);
        }
    }

    static async Task UpsertTracked<[DynamicallyAccessedMembers(EntityTypeMembers.Required)] TEntity>(this DbSet<TEntity> dbSet, TEntity item, CancellationToken cancellationToken)
        where TEntity : class
    {
        var entityType = dbSet.EntityType;
        var keyProperties = entityType.FindPrimaryKey()?.Properties;

        if (keyProperties?.Any() != true)
        {
            throw new EntityDoesNotHavePrimaryKey(typeof(TEntity));
        }

        var existingInLocal = dbSet.Local.FirstOrDefault(e => KeysMatch(e, item, keyProperties));
        if (existingInLocal is not null)
        {
            dbSet.UpdateExisting(item, existingInLocal);
            return;
        }

        var keyValues = GetKeyValues(item, keyProperties);
        var existingInDb = await dbSet.FindAsync(keyValues, cancellationToken);

        if (existingInDb is not null)
        {
            dbSet.UpdateExisting(item, existingInDb);
        }
        else
        {
            dbSet.Add(item);
        }
    }

    static void UpdateExisting<[DynamicallyAccessedMembers(EntityTypeMembers.Required)] TEntity>(this DbSet<TEntity> dbSet, TEntity newEntity, TEntity existingEntity)
        where TEntity : class
    {
        var entry = dbSet.Entry(existingEntity);
        entry.CurrentValues.SetValues(newEntity);
        entry.State = EntityState.Modified;
    }

    static bool KeysMatch<TEntity>(TEntity entity1, TEntity entity2, IReadOnlyList<Metadata.IProperty> keyProperties)
        where TEntity : class
    {
        foreach (var keyProperty in keyProperties)
        {
            var value1 = keyProperty.GetGetter().GetClrValue(entity1);
            var value2 = keyProperty.GetGetter().GetClrValue(entity2);

            if (!Equals(value1, value2))
            {
                return false;
            }
        }

        return true;
    }

    static object?[] GetKeyValues<TEntity>(TEntity entity, IReadOnlyList<Metadata.IProperty> keyProperties)
        where TEntity : class
    {
        var keyValues = new object?[keyProperties.Count];
        for (var i = 0; i < keyProperties.Count; i++)
        {
            keyValues[i] = keyProperties[i].GetGetter().GetClrValue(entity);
        }

        return keyValues;
    }
}
