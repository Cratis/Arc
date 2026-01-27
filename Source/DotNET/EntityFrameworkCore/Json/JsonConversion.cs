// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Json;
using Cratis.Strings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cratis.Arc.EntityFrameworkCore.Json;

/// <summary>
/// Provides JSON conversion capabilities for entity properties.
/// </summary>
public static class JsonConversion
{
    static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerOptions.Default)
    {
        WriteIndented = false,
        Converters =
        {
            new ConceptAsJsonConverterFactory()
        }
    };

    /// <summary>
    /// Applies JSON conversion to all properties marked with the <see cref="JsonAttribute"/> in the specified <see cref="ModelBuilder"/>.
    /// </summary>
    /// <param name="modelBuilder">The model builder to apply the JSON conversion to.</param>
    /// <param name="entityTypes">The entity types to apply the JSON conversion to.</param>
    /// <param name="databaseType">The database provider, if specific configuration is needed.</param>
    /// <returns>A collection of types that only appear in JSON-converted properties.</returns>
    public static IEnumerable<Type> ApplyJsonConversion(this ModelBuilder modelBuilder, IEnumerable<IMutableEntityType> entityTypes, DatabaseType databaseType)
    {
        var allEntityTypes = entityTypes.ToArray();
        var entityTypesWithJson = allEntityTypes.Where(t => t.HasJsonProperties()).ToArray();

        var typesInJsonProperties = new HashSet<Type>();
        var typesInNonJsonProperties = new HashSet<Type>();

        foreach (var entityType in allEntityTypes)
        {
            foreach (var property in entityType.ClrType.GetProperties())
            {
                var hasJsonAttribute = Attribute.IsDefined(property, typeof(JsonAttribute), inherit: true);
                var propertyType = property.PropertyType;

                if (hasJsonAttribute)
                {
                    typesInJsonProperties.Add(propertyType);
                }
                else
                {
                    typesInNonJsonProperties.Add(propertyType);
                }
            }

            foreach (var constructor in entityType.ClrType.GetConstructors())
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    var hasJsonAttribute = Attribute.IsDefined(parameter, typeof(JsonAttribute), inherit: true);
                    var parameterType = parameter.ParameterType;

                    if (hasJsonAttribute)
                    {
                        typesInJsonProperties.Add(parameterType);
                    }
                    else
                    {
                        typesInNonJsonProperties.Add(parameterType);
                    }
                }
            }
        }

        foreach (var entityType in entityTypesWithJson)
        {
            var entityTypeBuilder = modelBuilder.Entity(entityType.Name);
            entityTypeBuilder.ApplyJsonConversion(databaseType);
        }

        return typesInJsonProperties.Except(typesInNonJsonProperties);
    }

    /// <summary>
    /// Checks if the entity has any JSON properties.
    /// </summary>
    /// <param name="entity">The entity type builder to check for JSON properties.</param>
    /// <returns>True if the entity has JSON properties; otherwise, false.</returns>
    public static bool HasJsonProperties(this IMutableEntityType entity) =>
        entity.ClrType.GetProperties()
            .Any(p => Attribute.IsDefined(p, typeof(JsonAttribute), inherit: true)) ||
        entity.ClrType.GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Any(p => Attribute.IsDefined(p, typeof(JsonAttribute), inherit: true));

    /// <summary>
    /// Applies JSON conversion to properties of a specific entity through its builder.
    /// </summary>
    /// <param name="entityTypeBuilder">The entity type builder to apply the JSON conversion to.</param>
    /// <param name="databaseType">The database provider, if specific configuration is needed.</param>
    public static void ApplyJsonConversion(this EntityTypeBuilder entityTypeBuilder, DatabaseType databaseType)
    {
        var propertiesWithAttribute = entityTypeBuilder.Metadata.ClrType.GetProperties()
            .Where(p => Attribute.IsDefined(p, typeof(JsonAttribute), inherit: true))
            .Select(p => p.Name)
            .ToHashSet();

        var parametersWithAttribute = entityTypeBuilder.Metadata.ClrType.GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Where(p => Attribute.IsDefined(p, typeof(JsonAttribute), inherit: true))
            .Select(p => p.Name!)
            .ToHashSet();

        var allPropertiesWithJson = propertiesWithAttribute
            .Union(parametersWithAttribute.Select(static name => name.ToPascalCase()))
            .Distinct()
            .ToList();

        foreach (var propertyName in allPropertiesWithJson)
        {
            var property = entityTypeBuilder.Metadata.ClrType.GetProperty(propertyName);
            if (property is null)
            {
                continue;
            }

            var propertyBuilder = entityTypeBuilder.Property(property.Name);
            var converterType = typeof(JsonValueConverter<>).MakeGenericType(property.PropertyType);
            var comparerType = typeof(JsonValueComparer<>).MakeGenericType(property.PropertyType);
            var converter = Activator.CreateInstance(converterType) as ValueConverter;
            var comparer = Activator.CreateInstance(comparerType) as ValueComparer;

            propertyBuilder.HasConversion(converter);
            propertyBuilder.Metadata.SetValueConverter(converter);
            propertyBuilder.Metadata.SetValueComparer(comparer);
            switch (databaseType)
            {
                case DatabaseType.Sqlite:
                    propertyBuilder.HasColumnType("TEXT");
                    break;
                case DatabaseType.SqlServer:
                    propertyBuilder.HasColumnType("nvarchar(max)");
                    break;
                case DatabaseType.PostgreSql:
                    propertyBuilder.HasColumnType("jsonb");
                    break;
            }
        }
    }

    sealed class JsonValueConverter<T>() : ValueConverter<T?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, _jsonSerializerOptions),
            v => v == null ? default : JsonSerializer.Deserialize<T>(v, _jsonSerializerOptions))
        where T : class;

    sealed class JsonValueComparer<T>() : ValueComparer<T?>(
            (a, b) => JsonEquals(a, b, _jsonSerializerOptions),
            v => v == null ? 0 : JsonSerializer.Serialize(v, _jsonSerializerOptions).GetHashCode(),
            v => v == null ? default : JsonSerializer.Deserialize<T>(
                        JsonSerializer.Serialize(v, _jsonSerializerOptions), _jsonSerializerOptions))
            where T : class
    {
        static bool JsonEquals(T? a, T? b, JsonSerializerOptions opt)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            return JsonSerializer.Serialize(a, opt) == JsonSerializer.Serialize(b, opt);
        }
    }
}
