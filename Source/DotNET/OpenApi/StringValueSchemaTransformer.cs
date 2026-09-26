// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi;

/// <summary>
/// Restores string schemas for built-in values handled by Arc's JSON converters.
/// </summary>
public class StringValueSchemaTransformer : IOpenApiSchemaTransformer
{
    /// <inheritdoc/>
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
        if (schema.Type is not null || (type != typeof(DateOnly) && type != typeof(TimeOnly) && type != typeof(Uri) && type != typeof(Type)))
        {
            return Task.CompletedTask;
        }

        var converter = context.JsonTypeInfo.Options.Converters.FirstOrDefault(candidate => candidate.CanConvert(type));
        if (converter is DateOnlyJsonConverter or TimeOnlyJsonConverter or UriJsonConverter or TypeJsonConverter)
        {
            schema.Type = JsonSchemaType.String;
            if (context.JsonPropertyInfo?.IsGetNullable == true || Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) is not null)
            {
                schema.Type |= JsonSchemaType.Null;
            }
        }

        return Task.CompletedTask;
    }
}
