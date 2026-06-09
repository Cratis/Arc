// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Concepts;
using Cratis.Geospatial;
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

    static readonly ValueConverter<Point, string> _pointValueConverter = new(
        point => JsonSerializer.Serialize(point),
        str => JsonSerializer.Deserialize<Point>(str) ?? new Point(0, 0));

    static readonly ValueConverter<LineString, string> _lineStringValueConverter = new(
        linestring => JsonSerializer.Serialize(linestring),
        str => JsonSerializer.Deserialize<LineString>(str) ?? new LineString(Array.Empty<Point>()));

    static readonly ValueConverter<Polygon, string> _polygonValueConverter = new(
        polygon => JsonSerializer.Serialize(polygon),
        str => JsonSerializer.Deserialize<Polygon>(str) ?? new Polygon(new LinearRing(Array.Empty<Point>()), Array.Empty<LinearRing>()));

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

    /// <summary>
    /// Configures the property to use a value conversion for Point types.
    /// </summary>
    /// <param name="propertyBuilder">The property builder to configure.</param>
    /// <returns>The configured property builder.</returns>
    public static PropertyBuilder AsPoint(this PropertyBuilder propertyBuilder)
    {
        propertyBuilder.HasConversion(_pointValueConverter);
        return propertyBuilder;
    }

    /// <summary>
    /// Configures the property to use a value conversion for LineString types.
    /// </summary>
    /// <param name="propertyBuilder">The property builder to configure.</param>
    /// <returns>The configured property builder.</returns>
    public static PropertyBuilder AsLineString(this PropertyBuilder propertyBuilder)
    {
        propertyBuilder.HasConversion(_lineStringValueConverter);
        return propertyBuilder;
    }

    /// <summary>
    /// Configures the property to use a value conversion for Polygon types.
    /// </summary>
    /// <param name="propertyBuilder">The property builder to configure.</param>
    /// <returns>The configured property builder.</returns>
    public static PropertyBuilder AsPolygon(this PropertyBuilder propertyBuilder)
    {
        propertyBuilder.HasConversion(_polygonValueConverter);
        return propertyBuilder;
    }

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
