// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi.for_EnumSchemaTransformer.when_transforming_async;

public class with_enum_type : given.an_enum_schema_and_context
{
    void Establish()
    {
        _transformer = new EnumSchemaTransformer();
        _schema = new OpenApiSchema
        {
            Enum = []
        };
        _schema.Enum.Add(JsonValue.Create(0));
        _schema.Enum.Add(JsonValue.Create(1));
        _schema.Enum.Add(JsonValue.Create(2));
        SetupContextForType(typeof(for_EnumSchemaTransformer.given.TestEnum));
    }

    async Task Because() => await _transformer.TransformAsync(_schema, _context, CancellationToken.None);

    [Fact] void should_have_three_enum_values() => _schema.Enum.Count.ShouldEqual(3);
    [Fact] void should_contain_first_as_string() => _schema.Enum.Select(e => e.ToString()).ShouldContain("First");
    [Fact] void should_contain_second_as_string() => _schema.Enum.Select(e => e.ToString()).ShouldContain("Second");
    [Fact] void should_contain_third_as_string() => _schema.Enum.Select(e => e.ToString()).ShouldContain("Third");
}
