// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi.for_EnumSchemaTransformer.when_transforming_async;

public class with_string_enum_converter : given.an_enum_schema_and_context
{
    void Establish()
    {
        _schema = new OpenApiSchema { Type = JsonSchemaType.Integer };
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        SetupContextForType(typeof(for_EnumSchemaTransformer.given.TestEnum), options);
    }

    async Task Because() => await _transformer.TransformAsync(_schema, _context, CancellationToken.None);

    [Fact] void should_be_a_string_schema() => _schema.Type.ShouldEqual(JsonSchemaType.String);
    [Fact] void should_contain_string_values() => _schema.Enum.Select(value => value.ToJsonString()).ShouldEqual(["\"First\"", "\"Second\"", "\"Third\""]);
}
