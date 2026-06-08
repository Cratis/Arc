// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Extensions for configuring properties in Entity Framework Core.
/// </summary>
public static class PropertyExtensions
{
    static readonly ValueConverter<Guid, string> _guidValueConverter = new(
        guid => guid.ToString("D"),
        str => Guid.Parse(str));

#if COORDINATE_TYPE_AVAILABLE
    // NOTE: This converter is ready for when Cratis.Fundamentals includes the Coordinate type
    // from Cratis.Geospatial namespace. Remove the #if/#endif when the type is available.
    using System.Text.Json;
    using Cratis.Geospatial;
    static readonly ValueConverter<Coordinate, string> _coordinateValueConverter = new(
        coord => JsonSerializer.Serialize(coord),
        str => JsonSerializer.Deserialize<Coordinate>(str));
#endif

    /// <summary>
    /// Configures the property to use a GUID representation that is compatible across different database providers.
    /// </summary>
    /// <param name="propertyBuilder">The property builder to configure.</param>
    /// <param name="databaseType">The database provider type.</param>
    /// <returns>The configured property builder.</returns>
    public static PropertyBuilder AsGuid(this PropertyBuilder propertyBuilder, DatabaseType databaseType)
    {
        if (propertyBuilder.Metadata.ClrType.IsConcept())
        {
            return propertyBuilder.AsConcept(databaseType);
        }

        if (databaseType == DatabaseType.Sqlite)
        {
            propertyBuilder.HasConversion(_guidValueConverter);
        }

        return propertyBuilder;
    }

#if COORDINATE_TYPE_AVAILABLE
    // NOTE: This method is ready for when Cratis.Fundamentals includes the Coordinate type
    // from Cratis.Geospatial namespace. Remove the #if/#endif when the type is available.
    /// <summary>
    /// Configures the property to use a value conversion for Coordinate types.
    /// </summary>
    /// <param name="propertyBuilder">The property builder to configure.</param>
    /// <param name="databaseType">The database provider type.</param>
    /// <returns>The configured property builder.</returns>
    public static PropertyBuilder AsCoordinate(this PropertyBuilder propertyBuilder, DatabaseType databaseType)
    {
        propertyBuilder.HasConversion(_coordinateValueConverter);
        return propertyBuilder;
    }
#endif

    /// <summary>
    /// Configures the property to use a value conversion for concept types.
    /// </summary>
    /// <param name="propertyBuilder">The property builder to configure.</param>
    /// <param name="databaseType">The database provider type.</param>
    /// <returns>The configured property builder.</returns>
    public static PropertyBuilder AsConcept(this PropertyBuilder propertyBuilder, DatabaseType databaseType)
    {
        var propertyType = propertyBuilder.Metadata.ClrType;
        if (!propertyType.IsConcept())
        {
            return propertyBuilder;
        }

        var conceptValueType = propertyType.GetConceptValueType();

        var converterType = typeof(ConceptAsValueConverter<,>).MakeGenericType(propertyType, conceptValueType);
        var comparerType = typeof(ConceptAsValueComparer<,>).MakeGenericType(propertyType, conceptValueType);

        if (conceptValueType == typeof(Guid) && databaseType == DatabaseType.Sqlite)
        {
            converterType = typeof(GuidConceptAsValueConverter<,>).MakeGenericType(propertyType, conceptValueType);
        }

        var converter = Activator.CreateInstance(converterType) as ValueConverter;
        var comparer = Activator.CreateInstance(comparerType) as ValueComparer;

        propertyBuilder.HasConversion(converter);
        propertyBuilder.Metadata.SetValueConverter(converter);
        propertyBuilder.Metadata.SetValueComparer(comparer);

        return propertyBuilder;
    }

    sealed class GuidConceptAsValueConverter<TConcept, TPrimitive>() : ValueConverter<TConcept, string>(
        v => v.Value.ToString("D"),
        v => (TConcept)ConceptFactory.CreateConceptInstance(typeof(TConcept), Guid.Parse(v)))
        where TConcept : notnull, ConceptAs<Guid>
        where TPrimitive : notnull, IComparable;

    sealed class ConceptAsValueConverter<TConcept, TPrimitive>() : ValueConverter<TConcept, TPrimitive>(
        v => v.Value,
        v => (TConcept)ConceptFactory.CreateConceptInstance(typeof(TConcept), v))
        where TConcept : notnull, ConceptAs<TPrimitive>
        where TPrimitive : notnull, IComparable;

    sealed class ConceptAsValueComparer<TConcept, TPrimitive>() : ValueComparer<TConcept>(
        (l, r) => l!.Equals(r),
        v => v.GetHashCode())
        where TConcept : notnull, ConceptAs<TPrimitive>
        where TPrimitive : notnull, IComparable;
}
