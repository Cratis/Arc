// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Arc.EntityFrameworkCore.Json;
using Cratis.Arc.EntityFrameworkCore.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Base class for DbContexts that needs standard things configured.
/// </summary>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
public class BaseDbContext(DbContextOptions options) : DbContext(options)
{
    readonly DbContextOptions _options = options;

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // IEntityTypeRegistrar is only available when using AddDbContext, not when directly instantiating the DbContext
        // We need to access the ApplicationServiceProvider from the options because the internal service provider
        // doesn't contain application services directly
        var coreOptions = _options.FindExtension<CoreOptionsExtension>();
        var registrar = coreOptions?.ApplicationServiceProvider?.GetService(typeof(IEntityTypeRegistrar)) as IEntityTypeRegistrar;
        registrar?.RegisterEntityMaps(this, modelBuilder);

        var entityTypes = modelBuilder.Model.GetEntityTypes();
        var dbSetTypes = GetType()
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .ToArray();

        var entityTypesForConverters = entityTypes
            .Where(IsRelevantForConverters(dbSetTypes))
            .ToArray();

        var databaseType = this.GetConfiguredDatabaseType();
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

    static Func<IMutableEntityType, bool> IsRelevantForConverters(Type[] dbSetTypes) => et =>
        et.IsOwned() ||
        dbSetTypes.Contains(et.ClrType) ||
        dbSetTypes.Any(dbSetType =>
            dbSetType.GetProperties().Any(p =>
                p.PropertyType == et.ClrType ||
                (p.PropertyType.IsGenericType &&
                 p.PropertyType.GetGenericArguments().Contains(et.ClrType))));
}
