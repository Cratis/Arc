// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi.for_ConceptSchemaTransformer.when_transforming_async;

public class with_non_concept_type : given.a_concept_schema_and_context
{
    JsonSchemaType _originalType;

    void Establish()
    {
        _transformer = new ConceptSchemaTransformer();
        _originalType = JsonSchemaType.String;
        _schema = new OpenApiSchema { Type = _originalType };
        SetupContextForType(typeof(string));
    }

    async Task Because() => await _transformer.TransformAsync(_schema, _context, CancellationToken.None);

    [Fact] void should_not_change_type() => _schema.Type.ShouldEqual(_originalType);
}
