// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Represents an <see cref="ICanResolveReadModelForCommand"/> that resolves read models backed by Entity Framework Core
/// through their owning <see cref="ReadOnlyDbContext"/>, keyed by the command's resolved key.
/// </summary>
/// <param name="readModelToDbContext">A map from each read model entity type to the DbContext type that owns it.</param>
public class EntityFrameworkReadModelForCommandResolver(IReadOnlyDictionary<Type, Type> readModelToDbContext) : ICanResolveReadModelForCommand
{
    /// <inheritdoc/>
    public IEnumerable<Type> ReadModelTypes { get; } = [.. readModelToDbContext.Keys];

    /// <inheritdoc/>
    /// <remarks>
    /// A <see cref="DbSet{TEntity}"/> on a read model <see cref="ReadOnlyDbContext"/> in the application carries each of
    /// these read models, which is what makes Entity Framework Core own them.
    /// <para>
    /// What follows from declaring: an instance resolved this way is materialized by Entity Framework Core's own entity
    /// model, so it never meets the MongoDB driver and no MongoDB serialization customization applies to it, even where
    /// the same customization is visibly at work on the query side.
    /// </para>
    /// </remarks>
    public ReadModelForCommandOwnership Ownership => ReadModelForCommandOwnership.Declared;

    /// <summary>
    /// Discovers the read model entity types carried by the given DbContext types and maps each to its owning DbContext.
    /// </summary>
    /// <param name="dbContextTypes">The <see cref="ReadOnlyDbContext"/> types to inspect.</param>
    /// <returns>A map from each read model entity type to the DbContext type that owns it.</returns>
    /// <remarks>
    /// A read model entity is one carried by a <see cref="DbSet{TEntity}"/> whose entity type is marked with
    /// <c>[ReadModel]</c>. When the same entity type appears in more than one DbContext the first one discovered owns it.
    /// </remarks>
    public static IReadOnlyDictionary<Type, Type> DiscoverReadModelTypes(IEnumerable<Type> dbContextTypes)
    {
        var map = new Dictionary<Type, Type>();
        foreach (var dbContextType in dbContextTypes)
        {
            var entityTypes = dbContextType.GetProperties()
                .Where(property => property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Select(property => property.PropertyType.GetGenericArguments()[0])
                .Where(entityType => entityType.IsReadModel());

            foreach (var entityType in entityTypes)
            {
                map.TryAdd(entityType, dbContextType);
            }
        }

        return map;
    }

    /// <inheritdoc/>
    /// <exception cref="UnableToResolveReadModelFromCommandContext">Thrown when the command carries no usable key to resolve the read model by; it surfaces as a validation failure (HTTP 400).</exception>
    /// <exception cref="EntityDoesNotHavePrimaryKey">Thrown when the read model entity has no single-property primary key to resolve by.</exception>
    public async Task<object?> Resolve(Type readModelType, CommandContext commandContext)
    {
        var resolvedKey = commandContext.GetResolvedKey();
        if (string.IsNullOrEmpty(resolvedKey))
        {
            // A read model is keyed by the command's resolved key, so an absent key can never resolve one — for a
            // nullable and a non-nullable dependency alike. That is invalid client input, so it surfaces as a
            // validation failure (HTTP 400) rather than null or an unhandled server error. A valid-but-not-found read
            // model still resolves to null below.
            throw new UnableToResolveReadModelFromCommandContext(readModelType);
        }

        var dbContext = (DbContext)commandContext.ServiceProvider!.GetRequiredService(readModelToDbContext[readModelType]);
        var primaryKey = dbContext.Model.GetEntityTypes().FirstOrDefault(entityType => entityType.ClrType == readModelType)?.FindPrimaryKey();
        if (primaryKey is null || primaryKey.Properties.Count != 1)
        {
            throw new EntityDoesNotHavePrimaryKey(readModelType);
        }

        var key = resolvedKey.ConvertTo(primaryKey.Properties[0].ClrType);

        // A never-materialized read model resolves to null; command-side code can inject a nullable read model and
        // treat null as "does not exist", while a non-nullable dependency is surfaced as a validation failure (HTTP 400).
        return await dbContext.FindAsync(readModelType, key);
    }
}
