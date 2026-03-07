// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi;

/// <summary>
/// Represents an implementation of <see cref="IOpenApiSchemaTransformer"/> that removes properties that are decorated with <see cref="FromRouteAttribute"/> or <see cref="FromQueryAttribute"/>.
/// </summary>
public class FromRequestSchemaTransformer : IOpenApiSchemaTransformer
{
    /// <inheritdoc/>
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        var parameters = type.GetConstructors().SelectMany(_ => _.GetParameters()).ToArray();

        bool HasConstructorParameterWithRequestParameter(PropertyInfo propertyInfo) =>
            parameters.Any(_ => _.Name == propertyInfo.Name && (_.HasAttribute<FromRouteAttribute>() || _.HasAttribute<FromQueryAttribute>()));

        var properties = type.GetProperties()
            .Where(_ =>
                _.HasAttribute<FromRouteAttribute>() ||
                _.HasAttribute<FromQueryAttribute>() ||
                HasConstructorParameterWithRequestParameter(_))
            .ToList();

        if (properties.Count > 0)
        {
            foreach (var property in properties)
            {
                var propertyToRemove = schema.Properties?.SingleOrDefault(_ => _.Key.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase));
                if (propertyToRemove.HasValue)
                {
                    schema.Properties?.Remove(propertyToRemove.Value.Key);
                }
            }
        }

        return Task.CompletedTask;
    }
}
