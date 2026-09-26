// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Arc.Queries;

namespace Cratis.Arc;

/// <summary>
/// Provides methods for converting values between different types.
/// </summary>
public static class ConverterExtensions
{
    /// <summary>
    /// The scalar types <see cref="ConvertToUnderlyingType"/> knows how to parse from a string, beyond
    /// <see cref="Type.IsPrimitive"/>.
    /// </summary>
    /// <remarks>
    /// This is the runtime's own notion of "primitive" for query-argument purposes - it intentionally does not
    /// match the proxy generator's TypeScript-shape-oriented primitive map (which also treats geospatial types
    /// as primitive because they map to known TypeScript types). Kept in
    /// sync with <see cref="IsEnumerableOfQueryArgumentElement"/> and <see cref="ConvertToUnderlyingType"/>.
    /// </remarks>
    static readonly HashSet<Type> _additionalQueryArgumentScalarTypes =
    [
        typeof(string),
        typeof(decimal),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid),
        typeof(DateOnly),
        typeof(TimeOnly),
        typeof(Uri),
        typeof(System.Text.Json.Nodes.JsonNode),
        typeof(System.Text.Json.Nodes.JsonObject),
        typeof(System.Text.Json.Nodes.JsonArray),
        typeof(System.Text.Json.JsonDocument)
    ];

    /// <summary>
    /// Converts a value to the specified target type, handling concepts, enumerables and nullable types.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="targetType">The target type to convert to.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="InvalidCollectionQueryArgument">The collection shape or an element is invalid.</exception>
    /// <remarks>
    /// Supports converting primitives to their <see cref="ConceptAs{T}"/> counterparts, and a delimited string
    /// (as produced by repeated query string keys collapsed into one value) into an enumerable of primitives,
    /// concepts, or enums.
    /// </remarks>
    public static object? ConvertTo(this object value, Type targetType)
    {
        if (targetType.IsNestedQueryArgumentCollection())
        {
            throw new InvalidCollectionQueryArgument(targetType, value);
        }

        if (value is null)
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        if (targetType.IsEnumerableOfQueryArgumentElement(out var elementType))
        {
            return ConvertToEnumerable(value, targetType, elementType);
        }

        // If the value is already the target type, return it directly
        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        // Handle concepts first
        if (targetType.IsConcept())
        {
            var underlyingType = targetType.GetConceptValueType();
            var convertedValue = ConvertToUnderlyingType(value, underlyingType);
            if (convertedValue is not null)
            {
                return ConceptFactory.CreateConceptInstance(targetType, convertedValue);
            }
            return null;
        }

        return ConvertToUnderlyingType(value, targetType);
    }

    /// <summary>
    /// Determines whether a type is an enumerable whose element type is individually convertible via
    /// <see cref="ConvertTo"/> - a primitive, a concept, or an enum.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="elementType">The element type, when the method returns true.</param>
    /// <returns>True if the type qualifies; otherwise false.</returns>
    /// <remarks>
    /// Mirrors <c>TypeExtensions.IsEnumerableOfPrimitiveOrConcept</c> in the proxy generator
    /// (Source/DotNET/Tools/ProxyGenerator/TypeExtensions.cs) - the client and the server must agree on which
    /// parameters the caller supplies versus which are injected dependencies, so a caller-supplied collection the
    /// generator emits a proxy for must also be one this runtime accepts as a query argument rather than asking the
    /// container to resolve it.
    /// <para>
    /// The two cannot share source: the generator classifies types loaded through a <c>MetadataLoadContext</c>
    /// (metadata-only, with its own type-name-based concept detection), while this runs against real loaded types
    /// using the framework's own <c>Cratis.Concepts</c> <c>IsConcept()</c>. Both must classify the same shapes as
    /// query arguments - see <c>for_ModelBoundQueryPerformer/when_getting_parameters</c> in Arc.Core.Specs and
    /// <c>for_TypeExtensions/when_checking_is_enumerable_of_primitive_or_concept</c> in ProxyGenerator.Specs for the
    /// specs that pin the two predicates to the same shapes.
    /// </para>
    /// </remarks>
    internal static bool IsEnumerableOfQueryArgumentElement(this Type type, out Type elementType)
    {
        elementType = typeof(object);

        if (!TryGetEnumerableElementType(type, out var candidateElementType))
        {
            return false;
        }

        elementType = candidateElementType;
        var scalarType = Nullable.GetUnderlyingType(elementType) ?? elementType;
        return scalarType.IsEnum || scalarType.IsConcept() || IsQueryArgumentScalar(scalarType);
    }

    /// <summary>
    /// Determines whether a collection contains another collection as its element type, excluding supported JSON nodes.
    /// </summary>
    /// <param name="type">The collection type.</param>
    /// <returns>True if the element is itself a collection.</returns>
    internal static bool IsNestedQueryArgumentCollection(this Type type) =>
        TryGetEnumerableElementType(type, out var elementType) &&
        !typeof(JsonNode).IsAssignableFrom(elementType) &&
        TryGetEnumerableElementType(elementType, out _);

    static bool IsQueryArgumentScalar(Type type) =>
        type.IsPrimitive || _additionalQueryArgumentScalarTypes.Contains(type);

    static bool TryGetEnumerableElementType(Type type, out Type elementType)
    {
        elementType = typeof(object);

        if (type == typeof(string))
        {
            return false;
        }

        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            return true;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            elementType = type.GetGenericArguments()[0];
            return true;
        }

        var enumerableInterface = Array.Find(
            type.GetInterfaces(),
            candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (enumerableInterface is not null)
        {
            elementType = enumerableInterface.GetGenericArguments()[0];
            return true;
        }

        return false;
    }

    /// <summary>
    /// Converts a value into an enumerable of <paramref name="elementType"/>, splitting a delimited string
    /// (the shape a repeated query string key collapses into) and converting each part individually.
    /// </summary>
    /// <param name="value">The raw value - a comma-separated GET string or the individual QUERY body values.</param>
    /// <param name="targetType">The declared parameter type to satisfy.</param>
    /// <param name="elementType">The element type to convert each part to.</param>
    /// <returns>A collection assignable to <paramref name="targetType"/> for supported collection types.</returns>
    /// <exception cref="InvalidCollectionQueryArgument">An element is invalid or null for a non-nullable type.</exception>
    /// <remarks>
    /// A part that itself contains a literal comma cannot round-trip through this - the collapsed
    /// <c>IReadOnlyDictionary&lt;string, string&gt;</c> query representation has already lost the boundary between
    /// distinct query string values by the time it reaches here. This is a pre-existing limitation of that
    /// representation, not something introduced by collection support; it affects only string-shaped elements whose
    /// value can contain a comma.
    /// </remarks>
    static object ConvertToEnumerable(object value, Type targetType, Type elementType)
    {
        var stringValue = value.ToString() ?? string.Empty;
        var parts = value is IEnumerable values and not string
            ? values.Cast<object?>().ToArray()
            : stringValue.Length == 0
                ? []
                : stringValue.Split(',', StringSplitOptions.TrimEntries).Cast<object?>().ToArray();
        var array = Array.CreateInstance(elementType, parts.Length);
        for (var index = 0; index < parts.Length; index++)
        {
            var part = parts[index];
            if (part is null)
            {
                if (Nullable.GetUnderlyingType(elementType) is null)
                {
                    throw new InvalidCollectionQueryArgument(targetType, part);
                }

                array.SetValue(null, index);
                continue;
            }

            // Scalar ConvertTo deliberately retains its existing default-on-failure behavior. A collection
            // must instead distinguish a successful conversion from a default value returned for bad input.
            if (!TryConvertCollectionElement(part, elementType, out var converted))
            {
                throw new InvalidCollectionQueryArgument(targetType, part);
            }

            array.SetValue(converted, index);
        }

        if (targetType.IsInstanceOfType(array))
        {
            return array;
        }

        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
        foreach (var item in array)
        {
            list.Add(item);
        }

        if (targetType.IsInstanceOfType(list))
        {
            return list;
        }

        var setType = typeof(HashSet<>).MakeGenericType(elementType);
        if (targetType.IsAssignableFrom(setType))
        {
            return Activator.CreateInstance(setType, list)!;
        }

        var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
        var constructor = targetType.GetConstructor([enumerableType]);
        return constructor is not null ? constructor.Invoke([list]) : array;
    }

    static bool TryConvertCollectionElement(object value, Type elementType, out object? converted)
    {
        converted = null;
        if (elementType.IsInstanceOfType(value))
        {
            converted = value;
            return true;
        }

        var scalarType = Nullable.GetUnderlyingType(elementType) ?? elementType;
        var text = value.ToString();
        if (scalarType == typeof(string))
        {
            converted = text;
            return true;
        }

        if (typeof(JsonNode).IsAssignableFrom(scalarType) || scalarType == typeof(JsonDocument))
        {
            try
            {
                converted = scalarType == typeof(JsonDocument) ? JsonDocument.Parse(text!) : JsonNode.Parse(text!);
                return converted is not null && scalarType.IsInstanceOfType(converted);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        // A custom TypeConverter or a concept's underlying type can also return a default on failure.
        // Check that the input is valid by converting through the same scalar path without accepting
        // a fabricated default for an invalid value.
        if (scalarType.IsConcept())
        {
            var conceptValueType = scalarType.GetConceptValueType();
            if (!TryConvertCollectionElement(value, conceptValueType, out _))
            {
                return false;
            }
        }
        else if (!CanConvertCollectionElement(text, scalarType))
        {
            return false;
        }

        converted = value.ConvertTo(elementType);
        return converted is not null;
    }

    static bool CanConvertCollectionElement(string value, Type type)
    {
        try
        {
            if (type == typeof(int)) return int.TryParse(value, out _);
            if (type == typeof(long)) return long.TryParse(value, out _);
            if (type == typeof(short)) return short.TryParse(value, out _);
            if (type == typeof(byte)) return byte.TryParse(value, out _);
            if (type == typeof(bool)) return bool.TryParse(value, out _);
            if (type == typeof(float)) return float.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out _);
            if (type == typeof(double)) return double.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out _);
            if (type == typeof(decimal)) return decimal.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out _);
            if (type == typeof(DateTime)) return DateTime.TryParse(value, out _);
            if (type == typeof(DateTimeOffset)) return DateTimeOffset.TryParse(value, out _);
            if (type == typeof(Guid)) return Guid.TryParse(value, out _);
            if (type.IsEnum) return Enum.TryParse(type, value, true, out _);

            var converter = TypeDescriptor.GetConverter(type);
            return converter.CanConvertFrom(typeof(string)) && converter.ConvertFromString(value) is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    static object? ConvertToUnderlyingType(object value, Type targetType)
    {
        if (value is null)
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        // If the value is already the target type, return it directly
        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        var stringValue = value.ToString();
        if (string.IsNullOrEmpty(stringValue))
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (underlyingType == typeof(string))
        {
            return stringValue;
        }

        try
        {
            if (underlyingType == typeof(int))
                return int.Parse(stringValue);
            if (underlyingType == typeof(long))
                return long.Parse(stringValue);
            if (underlyingType == typeof(short))
                return short.Parse(stringValue);
            if (underlyingType == typeof(byte))
                return byte.Parse(stringValue);
            if (underlyingType == typeof(bool))
                return bool.Parse(stringValue);
            if (underlyingType == typeof(float))
                return float.Parse(stringValue, System.Globalization.CultureInfo.InvariantCulture);
            if (underlyingType == typeof(double))
                return double.Parse(stringValue, System.Globalization.CultureInfo.InvariantCulture);
            if (underlyingType == typeof(decimal))
                return decimal.Parse(stringValue, System.Globalization.CultureInfo.InvariantCulture);
            if (underlyingType == typeof(DateTime))
                return DateTime.Parse(stringValue);
            if (underlyingType == typeof(DateTimeOffset))
                return DateTimeOffset.Parse(stringValue);
            if (underlyingType == typeof(Guid))
                return Guid.Parse(stringValue);
            if (underlyingType.IsEnum)
                return Enum.Parse(underlyingType, stringValue, true);

            var converter = TypeDescriptor.GetConverter(underlyingType);
            if (converter.CanConvertFrom(typeof(string)))
            {
                return converter.ConvertFromString(stringValue);
            }
        }
        catch (Exception)
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
    }
}