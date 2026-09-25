// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi;

/// <summary>
/// Retains the declared properties of a polymorphic type when its converter suppresses schema inference.
/// </summary>
public class DerivedTypeSchemaTransformer : IOpenApiSchemaTransformer
{
    /// <inheritdoc/>
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (context.JsonTypeInfo.Options.Converters.FirstOrDefault(converter => converter.CanConvert(type)) is DerivedTypeJsonConverterFactory)
        {
            schema.Type = JsonSchemaType.Object;
            DeferredSchemas.Register(schema, context);
        }

        return Task.CompletedTask;
    }
}
