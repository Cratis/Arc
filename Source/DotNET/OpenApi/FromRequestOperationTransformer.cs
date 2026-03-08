// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ModelBinding;
using Cratis.Reflection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi;

/// <summary>
/// Represents an implementation of <see cref="IOpenApiOperationTransformer"/> that adds support for <see cref="FromRequestAttribute"/> attribute.
/// </summary>
public class FromRequestOperationTransformer : IOpenApiOperationTransformer
{
    /// <inheritdoc/>
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var methodInfo = context.GetMethodInfo();
        if (methodInfo is null)
        {
            return;
        }

        var parameterInfos = methodInfo.GetParameters();

        var parameters = context.Description.ParameterDescriptions.Where(_ => parameterInfos.Any(p =>
            p.Name == _.Name && p.HasAttribute<FromRequestAttribute>())).ToArray();
        if (parameters.Length == 0)
        {
            return;
        }

        foreach (var parameter in parameters)
        {
            var openApiParameter = operation.Parameters?.FirstOrDefault(_ => _.Name == parameter.Name);
            if (openApiParameter is not null)
            {
                operation.Parameters?.Remove(openApiParameter);
            }

            var schema = await context.GetOrCreateSchemaAsync(parameter.Type, parameter, cancellationToken);
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = schema
                    }
                }
            };
        }
    }
}
