// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi.for_ConceptSchemaTransformer.when_transforming_async.given;

public class a_concept_schema_and_context : Specification
{
    protected ConceptSchemaTransformer _transformer;
    protected OpenApiSchema _schema;
    protected OpenApiSchemaTransformerContext _context;

    void Establish()
    {
        _transformer = new ConceptSchemaTransformer();
        _schema = new OpenApiSchema
        {
            Type = JsonSchemaType.Object
        };
    }

    protected void SetupContextForType(Type type)
    {
        var jsonTypeInfo = JsonTypeInfo.CreateJsonTypeInfo(type, new JsonSerializerOptions());
        _context = new OpenApiSchemaTransformerContext
        {
            DocumentName = "test",
            JsonTypeInfo = jsonTypeInfo,
            ParameterDescription = null,
            JsonPropertyInfo = null,
            ApplicationServices = new TestServiceProvider()
        };
    }
}
