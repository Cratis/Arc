// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
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
        var elementType = type.IsArray ? type.GetElementType() :
            type.IsGenericType && context.JsonTypeInfo.Options.Converters.FirstOrDefault(converter => converter.CanConvert(type)) is EnumerableConceptAsJsonConverterFactory
                ? type.GetGenericArguments()[0] : null;
        if (elementType?.IsConcept() != true)
        {
            return;
        }

        schema.Type = JsonSchemaType.Array;
        schema.Items = await context.GetOrCreateSchemaAsync(elementType, null, cancellationToken);
    }
}
