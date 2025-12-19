// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ControllerBased;

/// <summary>
/// Extension methods for working with properties.
/// </summary>
public static class PropertyExtensions
{
    /// <summary>
    /// Check if a property is a request parameter which will make it part of the query string either as route variable or a query string parameter.
    /// </summary>
    /// <param name="property">Property to check.</param>
    /// <returns>True if it is a request parameter, false otherwise.</returns>
    public static bool IsRequestParameter(this PropertyInfo property)
    {
        var attributes = property.GetCustomAttributesData().Select(_ => _.AttributeType.Name);
        return attributes.Any(_ => _ == WellKnownTypes.FromRouteAttribute) ||
               attributes.Any(_ => _ == WellKnownTypes.FromQueryAttribute);
    }

    /// <summary>
    /// Check if a property is adorned with the FromQueryAttribute, which means we need to treat it as a query string parameter and include it in the route template.
    /// </summary>
    /// <param name="property">Method to check.</param>
    /// <returns>True if it is a from request parameter, false otherwise.</returns>
    public static bool IsFromQueryParameter(this PropertyInfo property)
    {
        var attributes = property.GetCustomAttributesData().Select(_ => _.AttributeType.Name);
        return attributes.Any(_ => _ == WellKnownTypes.FromQueryAttribute);
    }

    /// <summary>
    /// Convert a <see cref="PropertyInfo"/> to a <see cref="RequestParameterDescriptor"/>.
    /// </summary>
    /// <param name="propertyInfo">Parameter to convert.</param>
    /// <returns>Converted <see cref="RequestParameterDescriptor"/>.</returns>
    public static RequestParameterDescriptor ToRequestParameterDescriptor(this PropertyInfo propertyInfo)
    {
        var type = propertyInfo.PropertyType.GetTargetType();
        var optional = propertyInfo.IsOptional();
        var documentation = propertyInfo.GetDocumentation();
        return new RequestParameterDescriptor(
            propertyInfo.PropertyType,
            propertyInfo.Name!,
            type.Type,
            type.Constructor,
            optional,
            propertyInfo.IsFromQueryParameter(),
            documentation);
    }
}
