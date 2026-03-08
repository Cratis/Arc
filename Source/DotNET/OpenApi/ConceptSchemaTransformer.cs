// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi;

/// <summary>
/// Represents an implementation of <see cref="IOpenApiSchemaTransformer"/> that correctly provides the schema for <see cref="ConceptAs{T}"/>.
/// </summary>
public class ConceptSchemaTransformer : IOpenApiSchemaTransformer
{
    /// <inheritdoc/>
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (!type.IsConcept())
        {
            return Task.CompletedTask;
        }

        var valueType = type.GetConceptValueType();
        var newSchema = valueType.MapTypeToOpenApiPrimitiveType();

        schema.Type = newSchema.Type;
        schema.Format = newSchema.Format;

        return Task.CompletedTask;
    }
}
