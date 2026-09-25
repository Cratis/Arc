// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi;

/// <summary>
/// Marks dictionaries whose keys Arc serializes as JSON property names for expansion by <see cref="DeferredSchemaDocumentTransformer"/>.
/// </summary>
public class ComplexKeyDictionarySchemaTransformer : IOpenApiSchemaTransformer
{
    /// <inheritdoc/>
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (type.IsGenericType && context.JsonTypeInfo.Options.Converters.FirstOrDefault(converter => converter.CanConvert(type)) is ComplexKeyDictionaryJsonConverterFactory)
        {
            schema.Type = JsonSchemaType.Object;
            DeferredSchemas.Register(schema, context);
        }

        return Task.CompletedTask;
    }
}
