// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi;

/// <summary>
/// Restores object schemas for dictionaries whose keys Arc serializes as JSON property names.
/// </summary>
public class ComplexKeyDictionarySchemaTransformer : IOpenApiSchemaTransformer
{
    /// <inheritdoc/>
    public async Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (context.JsonTypeInfo.Options.Converters.FirstOrDefault(converter => converter.CanConvert(type)) is not ComplexKeyDictionaryJsonConverterFactory)
        {
            return;
        }

        var valueType = type.GetInterfaces().Append(type)
            .First(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            .GetGenericArguments()[1];
        schema.Type = JsonSchemaType.Object;
        schema.AdditionalProperties = await context.GetOrCreateSchemaAsync(valueType, null, cancellationToken);
    }
}
