// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Executes a single-statement upsert for scalar, single-table entities.
/// </summary>
internal static class NativeUpsert
{
    /// <summary>
    /// Attempts an atomic native upsert without attaching the entity.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="dbSet">The owning set.</param>
    /// <param name="item">The item to persist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Whether the mapping supports native execution.</returns>
    /// <exception cref="EntityDoesNotHavePrimaryKey">The entity has no primary key.</exception>
    internal static async Task<bool> TryExecute<TEntity>(DbSet<TEntity> dbSet, TEntity item, CancellationToken cancellationToken)
        where TEntity : class
    {
        var context = dbSet.GetService<ICurrentDbContext>().Context;
        var entityType = dbSet.EntityType;
        var keys = entityType.FindPrimaryKey()?.Properties ?? throw new EntityDoesNotHavePrimaryKey(typeof(TEntity));
        var sqlServer = context.Database.IsSqlServer();
        if ((!sqlServer && !context.Database.IsNpgsql() && !context.Database.IsSqlite()) || !CanUseNative(dbSet.Entry(item), sqlServer))
        {
            return false;
        }

        var table = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
        var properties = entityType.GetProperties().Where(_ => _.GetBeforeSaveBehavior() == PropertySaveBehavior.Save).ToArray();
        var helper = context.GetService<ISqlGenerationHelper>();
        var columns = properties.Select(_ => helper.DelimitIdentifier(_.GetColumnName(table)!)).ToArray();
        var keyColumns = keys.Select(_ => helper.DelimitIdentifier(_.GetColumnName(table)!)).ToArray();
        var updates = properties.Where(_ => !keys.Contains(_) && _.GetAfterSaveBehavior() == PropertySaveBehavior.Save)
            .Select(_ => helper.DelimitIdentifier(_.GetColumnName(table)!)).ToArray();
        var parameters = properties.Select((_, index) => helper.GenerateParameterName($"p{index}")).ToArray();
        var tableName = helper.DelimitIdentifier(table.Name, table.Schema);
        var sql = sqlServer
            ? Merge(tableName, columns, keyColumns, updates, parameters)
            : OnConflict(tableName, columns, keyColumns, updates, parameters);
        await using var command = context.Database.GetDbConnection().CreateCommand();
        var entry = dbSet.Entry(item);
        var values = properties.Select((property, index) => property.GetRelationalTypeMapping()
            .CreateParameter(command, parameters[index], entry.Property(property.Name).CurrentValue, property.IsNullable)).ToArray();
        await context.Database.ExecuteSqlRawAsync(sql, values, cancellationToken);
        return true;
    }

    static bool CanUseNative(EntityEntry entry, bool sqlServer)
    {
        var entityType = entry.Metadata;
        if (!entry.IsKeySet || entityType.BaseType is not null || entityType.GetDerivedTypes().Any() ||
            entityType.GetTableMappings().Count() != 1 || entityType.GetComplexProperties().Any() ||
            entityType.IsOwned() || entityType.GetNavigations().Any() || entityType.GetSkipNavigations().Any() ||
            entityType.GetProperties().Any(_ => _.IsShadowProperty()))
        {
            return false;
        }

        return entityType.GetProperties().All(property =>
            !(sqlServer && SqlServerPropertyExtensions.GetValueGenerationStrategy(property) == SqlServerValueGenerationStrategy.IdentityColumn) &&
            !(property.ValueGenerated != ValueGenerated.Never && property.GetBeforeSaveBehavior() == PropertySaveBehavior.Save &&
              Equals(entry.Property(property.Name).CurrentValue, property.Sentinel)));
    }

    static string OnConflict(string table, string[] columns, string[] keys, string[] updates, string[] parameters)
    {
        var action = updates.Length == 0
            ? "DO NOTHING"
            : $"DO UPDATE SET {string.Join(", ", updates.Select(_ => $"{_} = excluded.{_}"))}";
        return $"INSERT INTO {table} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", parameters)}) ON CONFLICT ({string.Join(", ", keys)}) {action};";
    }

    static string Merge(string table, string[] columns, string[] keys, string[] updates, string[] parameters)
    {
        var match = string.Join(" AND ", keys.Select(_ => $"target.{_} = source.{_}"));
        var update = updates.Length == 0
            ? string.Empty
            : $" WHEN MATCHED THEN UPDATE SET {string.Join(", ", updates.Select(_ => $"target.{_} = source.{_}"))}";
        return $"MERGE INTO {table} WITH (HOLDLOCK) AS target USING (VALUES ({string.Join(", ", parameters)})) AS source ({string.Join(", ", columns)}) ON {match}{update} WHEN NOT MATCHED THEN INSERT ({string.Join(", ", columns)}) VALUES ({string.Join(", ", columns.Select(_ => $"source.{_}"))});";
    }
}
