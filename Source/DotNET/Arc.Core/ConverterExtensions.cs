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
            return DefaultValueFor(targetType);
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
            var convertedValue = ConvertToUnderlyingType(value, underlyingType, returnNullOnFailure: true);
            if (convertedValue is not null)
            {
                return ConceptFactory.CreateConceptInstance(targetType, convertedValue);
            }
            return null;
        }

        return ConvertToUnderlyingType(value, targetType);
    }

    /// <summary>
    /// Converts a supplied query argument without accepting a fabricated default for invalid scalar input.
    /// Other callers of <see cref="ConvertTo"/> retain their existing conversion behavior.
    /// </summary>
    /// <param name="value">The supplied value.</param>
    /// <param name="targetType">The declared parameter type.</param>
    /// <param name="argumentName">The declared parameter name.</param>
    /// <param name="queryName">The query receiving the argument.</param>
    /// <returns>The converted value, or null for a missing value.</returns>
    /// <exception cref="InvalidQueryArgument">A supplied scalar or collection value cannot be converted.</exception>
    internal static object? ConvertQueryArgument(this object? value, Type targetType, string argumentName, FullyQualifiedQueryName queryName)
    {
        if (value is null)
        {
            return null;
        }

        if (targetType.IsNestedQueryArgumentCollection() || targetType.IsEnumerableOfQueryArgumentElement(out _))
        {
            try
            {
                return value.ConvertTo(targetType);
            }
            catch (InvalidCollectionQueryArgument)
            {
                throw new InvalidQueryArgument(argumentName, targetType, queryName);
            }
        }

        // Empty non-string scalars are treated as absent by the performer. String concepts can represent empty text.
        if (value is string { Length: 0 })
        {
            return targetType.IsConcept() && targetType.GetConceptValueType() == typeof(string)
                ? ConceptFactory.CreateConceptInstance(targetType, string.Empty)
                : value;
        }

        if (!TryConvertCollectionElement(value, targetType, out var converted))
        {
            throw new InvalidQueryArgument(argumentName, targetType, queryName);
        }

        return converted;
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
        else if (!TryParseScalar(text, scalarType, out var parsed) || parsed is null)
        {
            return false;
        }

        converted = value.ConvertTo(elementType);
        return converted is not null;
    }

    static object? ConvertToUnderlyingType(object value, Type targetType, bool returnNullOnFailure = false)
    {
        if (value is null)
        {
            return ConversionFailure(targetType, returnNullOnFailure);
        }

        // If the value is already the target type, return it directly
        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        var stringValue = value.ToString();
        if (string.IsNullOrEmpty(stringValue))
        {
            return ConversionFailure(targetType, returnNullOnFailure);
        }

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (underlyingType == typeof(string))
        {
            return stringValue;
        }

        return TryParseScalar(stringValue, underlyingType, out var result)
            ? result
            : ConversionFailure(targetType, returnNullOnFailure);
    }

    /// <summary>
    /// Parses a string into a scalar of the given type - the single place both scalar and collection element
    /// conversion parse text.
    /// </summary>
    /// <param name="value">The text to parse.</param>
    /// <param name="type">The non-nullable scalar type to parse into.</param>
    /// <param name="result">The parsed value, which a <see cref="TypeConverter"/> may leave null.</param>
    /// <returns>True if the text could be parsed; false if it is invalid or the type cannot be parsed from a string.</returns>
    static bool TryParseScalar(string value, Type type, out object? result)
    {
        result = null;
        try
        {
            result = ParseKnownScalar(value, type);
            if (result is not null)
            {
                return true;
            }

            var converter = TypeDescriptor.GetConverter(type);
            if (!converter.CanConvertFrom(typeof(string)))
            {
                return false;
            }

            result = converter.ConvertFromString(value);
            return true;
        }
        catch (Exception)
        {
            result = null;
            return false;
        }
    }

    static object? ParseKnownScalar(string value, Type type)
    {
        if (type == typeof(int))
            return int.Parse(value);
        if (type == typeof(long))
            return long.Parse(value);
        if (type == typeof(short))
            return short.Parse(value);
        if (type == typeof(byte))
            return byte.Parse(value);
        if (type == typeof(bool))
            return bool.Parse(value);
        if (type == typeof(float))
            return float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        if (type == typeof(double))
            return double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        if (type == typeof(decimal))
            return decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        if (type == typeof(DateTime))
            return DateTime.Parse(value);
        if (type == typeof(DateTimeOffset))
            return DateTimeOffset.Parse(value);
        if (type == typeof(Guid))
            return Guid.Parse(value);
        if (type.IsEnum)
            return Enum.Parse(type, value, true);

        return null;
    }

    static object? ConversionFailure(Type targetType, bool returnNullOnFailure) =>
        returnNullOnFailure ? null : DefaultValueFor(targetType);

    /// <summary>
    /// Gets the value an unconvertible input falls back to - the default of a value type, null otherwise.
    /// </summary>
    /// <param name="type">The type to get the default value for.</param>
    /// <returns>The boxed default of a value type; null for a reference type or <see cref="Nullable{T}"/>.</returns>
    static object? DefaultValueFor(Type type) =>
        type.IsValueType ? Activator.CreateInstance(type) : null;
}
