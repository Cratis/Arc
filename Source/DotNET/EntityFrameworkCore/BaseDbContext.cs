// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Arc.EntityFrameworkCore.Json;
using Cratis.Arc.EntityFrameworkCore.Mapping;
using Cratis.Concepts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Base class for DbContexts that needs standard things configured.
/// </summary>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
public class BaseDbContext(DbContextOptions options) : DbContext(options)
{
    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        this.GetService<IEntityTypeRegistrar>().RegisterEntityMaps(this, modelBuilder);

        var entityTypes = modelBuilder.Model.GetEntityTypes();
        var dbSetTypes = GetType()
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .ToArray();

        // Ignore ConceptAs types to prevent EF Core from treating them as entity types
        IgnoreConceptAsTypes(modelBuilder, dbSetTypes);

        var entityTypesForConverters = entityTypes
            .Where(IsRelevantForConverters(dbSetTypes))
            .ToArray();

        var databaseType = Database.GetDatabaseType();
        var typesOnlyInJsonProperties = modelBuilder.ApplyJsonConversion(entityTypesForConverters, databaseType);

        var entityTypesForOtherConverters = entityTypesForConverters
            .Where(et => !typesOnlyInJsonProperties.Contains(et.ClrType))
            .ToArray();

        var propertyTypesOnlyInJsonEntities = GetPropertyTypesOnlyInJsonEntities(
            entityTypesForOtherConverters,
            typesOnlyInJsonProperties);

        var finalEntityTypesForConverters = entityTypesForOtherConverters
            .Where(et => !propertyTypesOnlyInJsonEntities.Contains(et.ClrType))
            .ToArray();

        modelBuilder.ApplyConceptAsConversion(finalEntityTypesForConverters, databaseType);
        modelBuilder.ApplyGuidConversion(finalEntityTypesForConverters, databaseType);
        base.OnModelCreating(modelBuilder);
    }

    static HashSet<Type> GetPropertyTypesOnlyInJsonEntities(
        IMutableEntityType[] entityTypesForConverters,
        IEnumerable<Type> typesOnlyInJsonProperties)
    {
        var jsonEntityTypes = typesOnlyInJsonProperties.ToHashSet();
        var propertyTypesInJsonEntities = new HashSet<Type>();
        var propertyTypesInNonJsonEntities = new HashSet<Type>();

        foreach (var entityType in entityTypesForConverters)
        {
            var isJsonEntity = jsonEntityTypes.Contains(entityType.ClrType);

            foreach (var property in entityType.ClrType.GetProperties())
            {
                var propertyType = property.PropertyType;

                if (isJsonEntity)
                {
                    propertyTypesInJsonEntities.Add(propertyType);
                }
                else
                {
                    propertyTypesInNonJsonEntities.Add(propertyType);
                }
            }
        }

        return propertyTypesInJsonEntities.Except(propertyTypesInNonJsonEntities).ToHashSet();
    }

    static void IgnoreConceptAsTypes(ModelBuilder modelBuilder, Type[] dbSetTypes)
    {
        var conceptTypes = dbSetTypes
            .SelectMany(t => t.GetProperties())
            .Select(p => p.PropertyType)
            .Where(t => t.IsConcept())
            .Distinct()
            .ToArray();

        foreach (var conceptType in conceptTypes)
        {
            modelBuilder.Ignore(conceptType);
        }
    }

    static Func<IMutableEntityType, bool> IsRelevantForConverters(Type[] dbSetTypes) => et =>
        et.IsOwned() ||
        dbSetTypes.Contains(et.ClrType) ||
        dbSetTypes.Any(dbSetType =>
            dbSetType.GetProperties().Any(p =>
                p.PropertyType == et.ClrType ||
                (p.PropertyType.IsGenericType &&
                 p.PropertyType.GetGenericArguments().Contains(et.ClrType))));
}
