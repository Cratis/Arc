// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Extension methods for working with properties.
/// </summary>
public static class PropertyExtensions
{
    /// <summary>
    /// Convert a <see cref="PropertyInfo"/> to a <see cref="PropertyDescriptor"/>.
    /// </summary>
    /// <param name="property">Property to convert.</param>
    /// <returns><see cref="PropertyDescriptor"/>.</returns>
    public static PropertyDescriptor ToPropertyDescriptor(this PropertyInfo property)
    {
        var documentation = property.GetDocumentation();
        return ToPropertyDescriptor(property.PropertyType, property.Name, documentation);
    }

    /// <summary>
    /// Check if a property is optional - typically for arguments or properties.
    /// </summary>
    /// <param name="property">Property to check.</param>
    /// <returns>True if it is, false if not.</returns>
    public static bool IsOptional(this PropertyInfo property)
    {
        return property.CustomAttributes.Any(_ =>
            _.AttributeType.FullName?.StartsWith("System.Runtime.CompilerServices.NullableAttribute") ?? false);
    }

    /// <summary>
    /// Convert a <see cref="ParameterInfo"/> to a <see cref="PropertyDescriptor"/>.
    /// </summary>
    /// <param name="parameterInfo">Parameter to convert.</param>
    /// <returns><see cref="PropertyDescriptor"/>.</returns>
    public static PropertyDescriptor ToPropertyDescriptor(this ParameterInfo parameterInfo)
    {
        var documentation = parameterInfo.GetDocumentation();
        return ToPropertyDescriptor(parameterInfo.ParameterType, parameterInfo.Name!, documentation);
    }

    static PropertyDescriptor ToPropertyDescriptor(Type propertyType, string name, string? documentation = null)
    {
        var isEnumerable = false;
        var isNullable = propertyType.IsNullable();
        if (isNullable)
        {
            propertyType = propertyType.GetGenericArguments()[0];
        }

        // Special handling for JsonArray - treat it as an enumerable
        if (propertyType.FullName == typeof(System.Text.Json.Nodes.JsonArray).FullName)
        {
            isEnumerable = true;
        }
        else if (!propertyType.IsKnownType())
        {
            isEnumerable = propertyType.IsEnumerable();
            if (isEnumerable)
            {
                propertyType = propertyType.GetEnumerableElementType()!;
            }
        }

        var targetType = propertyType.GetTargetType();

        return new(
            propertyType,
            name,
            targetType.Type,
            targetType.Constructor,
            targetType.Module,
            isEnumerable,
            isNullable,
            propertyType.IsAPrimitiveType(),
            documentation);
    }
}
