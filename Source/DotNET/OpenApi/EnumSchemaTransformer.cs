// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi;

/// <summary>
/// Represents an implementation of <see cref="IOpenApiSchemaTransformer"/> that correctly provides the schema for enums as names instead of integers.
/// </summary>
public class EnumSchemaTransformer : IOpenApiSchemaTransformer
{
    /// <inheritdoc/>
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (type.IsEnum)
        {
            schema.Enum.Clear();
            Enum.GetNames(type)
                .ToList()
                .ForEach(name => schema.Enum.Add(JsonValue.Create(name)));
        }

        return Task.CompletedTask;
    }
}
