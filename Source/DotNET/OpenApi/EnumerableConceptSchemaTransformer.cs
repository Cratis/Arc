// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi;

/// <summary>
/// Restores array schemas for concept collections handled by Arc's JSON converters.
/// </summary>
public class EnumerableConceptSchemaTransformer : IOpenApiSchemaTransformer
{
    /// <inheritdoc/>
    public async Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (context.JsonTypeInfo.Options.Converters.FirstOrDefault(converter => converter.CanConvert(type)) is not EnumerableConceptAsJsonConverterFactory)
        {
            return;
        }

        schema.Type = JsonSchemaType.Array;
        schema.Items = await context.GetOrCreateSchemaAsync(type.GetGenericArguments()[0], null, cancellationToken);
    }
}
