// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi.for_EnumSchemaTransformer.when_transforming_async;

public class with_non_enum_type : given.an_enum_schema_and_context
{
    void Establish()
    {
        _transformer = new EnumSchemaTransformer();
        _schema = new OpenApiSchema
        {
            Enum = []
        };
        SetupContextForType(typeof(string));
    }

    async Task Because() => await _transformer.TransformAsync(_schema, _context, CancellationToken.None);

    [Fact] void should_not_modify_enum() => _schema.Enum.ShouldBeEmpty();
}
