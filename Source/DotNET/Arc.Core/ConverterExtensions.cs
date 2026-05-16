// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Cratis.Arc;

/// <summary>
/// Provides methods for converting values between different types.
/// </summary>
public static class ConverterExtensions
{
    /// <summary>
    /// Converts a value to the specified target type, handling concepts and nullable types.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="targetType">The target type to convert to.</param>
    /// <returns>The converted value.</returns>
    /// <remarks>
    /// Supports converting primitives to their <see cref="ConceptAs{T}"/> counterparts.
    /// </remarks>
    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "Activator.CreateInstance(targetType) on value-type fallback path; targetType.IsValueType ensures it has a default constructor. Source-generated type converters are the long-term fix (tracked in GitHub issue #2204).")]
    public static object? ConvertTo(this object value, Type targetType)
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

    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "Activator.CreateInstance(targetType) on value-type fallback path; targetType.IsValueType ensures it has a default constructor. Source-generated type converters are the long-term fix (tracked in GitHub issue #2204).")]
    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "TypeDescriptor.GetConverter uses reflection. Source-generated TypeConverter registration is the long-term fix (tracked in GitHub issue #2204 item 7).")]
    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "TypeDescriptor.GetConverter requires All members on the type. The targetType/underlyingType values come from user code at runtime; source-generated registration is the long-term fix (tracked in GitHub issue #2204 item 7).")]
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