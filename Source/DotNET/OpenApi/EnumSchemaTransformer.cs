// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi;

/// <summary>
/// Describes enums using the values written by the effective JSON converter.
/// </summary>
public class EnumSchemaTransformer : IOpenApiSchemaTransformer
{
    /// <inheritdoc/>
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (type.IsEnum)
        {
            var values = Enum.GetValues(type).Cast<object>()
                .Select(value => JsonSerializer.SerializeToElement(value, type, context.JsonTypeInfo.Options))
                .ToArray();
            if (values.Length > 0 && values.All(value => value.ValueKind == JsonValueKind.String))
            {
                schema.Type = JsonSchemaType.String;
            }
            else if (values.All(value => value.ValueKind == JsonValueKind.Number))
            {
                schema.Type = JsonSchemaType.Integer;
            }
            else
            {
                return Task.CompletedTask;
            }

            schema.Enum ??= [];
            schema.Enum.Clear();
            foreach (var value in values)
            {
                schema.Enum.Add(JsonValue.Create(value));
            }
        }

        return Task.CompletedTask;
    }
}
