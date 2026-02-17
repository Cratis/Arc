// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi.for_ConceptSchemaTransformer.when_transforming_async;

public class with_concept_type : given.a_concept_schema_and_context
{
    void Establish()
    {
        _transformer = new ConceptSchemaTransformer();
        _schema = new OpenApiSchema { Type = JsonSchemaType.Object };
        SetupContextForType(typeof(for_ConceptSchemaTransformer.given.TestConcept));
    }

    async Task Because() => await _transformer.TransformAsync(_schema, _context, CancellationToken.None);

    [Fact] void should_change_type_to_string() => _schema.Type.ShouldEqual(JsonSchemaType.String);
    [Fact] void should_set_format_to_uuid() => _schema.Format.ShouldEqual("uuid");
}
