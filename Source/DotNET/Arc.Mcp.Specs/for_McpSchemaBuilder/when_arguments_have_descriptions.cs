// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Arc.Mcp.for_McpSchemaBuilder.given;
using Cratis.Json;

namespace Cratis.Arc.Mcp.for_McpSchemaBuilder;

public class when_arguments_have_descriptions : Specification
{
    JsonNode _properties;

    void Because()
    {
        var options = new JsonSerializerOptions().ConfigureArcDefaults();
        options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        _properties = JsonNode.Parse(McpSchemaBuilder.ForCommand(typeof(DescribedCommand), options))!["properties"]!;
    }

    [Fact] void should_use_the_wire_property_name() => _properties["display_name"]!["description"]!.GetValue<string>().ShouldEqual("The display name.");
    [Fact] void should_keep_concept_descriptions_when_unwrapping() => _properties["customer"]!["description"]!.GetValue<string>().ShouldEqual("The customer identifier.");
    [Fact] void should_keep_enum_descriptions_when_replacing_the_schema() => _properties["priority"]!["description"]!.GetValue<string>().ShouldEqual("The priority.");
    [Fact] void should_keep_nullable_descriptions() => _properties["age"]!["description"]!.GetValue<string>().ShouldEqual("An optional age.");
    [Fact] void should_omit_blank_descriptions() => _properties["blank"]!["description"].ShouldBeNull();
    [Fact] void should_not_invent_descriptions() => _properties["undescribed"]!["description"].ShouldBeNull();
}
