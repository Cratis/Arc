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
    public async Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (context.JsonTypeInfo.Options.Converters.FirstOrDefault(converter => converter.CanConvert(type)) is not DerivedTypeJsonConverterFactory)
        {
            return;
        }

        schema.Type = JsonSchemaType.Object;
        schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
        foreach (var property in type.GetProperties())
        {
            var name = context.JsonTypeInfo.Options.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name;
            schema.Properties[name] = await context.GetOrCreateSchemaAsync(property.PropertyType, null, cancellationToken);
        }
    }
}
