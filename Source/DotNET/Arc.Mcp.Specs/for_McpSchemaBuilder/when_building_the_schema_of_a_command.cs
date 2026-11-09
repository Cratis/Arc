// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.Mcp.for_McpSchemaBuilder.given;

namespace Cratis.Arc.Mcp.for_McpSchemaBuilder;

public class when_building_the_schema_of_a_command : Specification
{
    JsonNode _schema;
    JsonNode _golden;

    void Because()
    {
        _schema = JsonNode.Parse(McpSchemaBuilder.ForCommand(typeof(SchemaCommand)))!;
        _golden = JsonNode.Parse(Goldens.Command)!;
    }

    [Fact] void should_match_the_golden_schema() => JsonNode.DeepEquals(_schema, _golden).ShouldBeTrue();
    [Fact] void should_describe_concepts_as_their_value_type() => _schema["properties"]!["name"]!["type"]!.GetValue<string>().ShouldEqual("string");
    [Fact] void should_describe_enums_as_their_names() => _schema["properties"]!["priority"]!["enum"]!.AsArray().Select(_ => _!.GetValue<string>()).ShouldContainOnly("Low", "High");
    [Fact] void should_not_require_nullable_values() => _schema["properties"]!["address"]!["required"]!.AsArray().Select(_ => _!.GetValue<string>()).ShouldContainOnly("street");
    [Fact] void should_not_advertise_an_ignored_idempotency_key() => _schema["properties"]!["idempotency_key"].ShouldBeNull();
}
