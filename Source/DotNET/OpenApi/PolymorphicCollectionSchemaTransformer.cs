// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization.Metadata;
using Cratis.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi;

/// <summary>
/// Marks collections of polymorphic types for item expansion by <see cref="DeferredSchemaDocumentTransformer"/>.
/// </summary>
public class PolymorphicCollectionSchemaTransformer : IOpenApiSchemaTransformer
{
    /// <inheritdoc/>
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var typeInfo = context.JsonTypeInfo;
        if (typeInfo.Kind == JsonTypeInfoKind.Enumerable && typeInfo.ElementType is { } elementType &&
            typeInfo.Options.Converters.FirstOrDefault(converter => converter.CanConvert(elementType)) is DerivedTypeJsonConverterFactory)
        {
            schema.Type = JsonSchemaType.Array;
            DeferredSchemas.Register(schema, context);
        }

        return Task.CompletedTask;
    }
}
