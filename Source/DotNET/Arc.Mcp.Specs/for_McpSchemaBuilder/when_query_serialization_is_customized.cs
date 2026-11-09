// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cratis.Arc.Mcp.for_McpSchemaBuilder.given;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Mcp.for_McpSchemaBuilder;

public class when_query_serialization_is_customized : Specification
{
    JsonNode _schema;

    void Because()
    {
        var options = new JsonSerializerOptions().ConfigureArcDefaults();
        options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.Converters.Insert(0, new JsonStringEnumConverter<Priority>(JsonNamingPolicy.SnakeCaseLower));
        QueryParameter[] parameters = [new("CustomerId", typeof(CustomerId), true), new("Priority", typeof(Priority))];
        _schema = JsonNode.Parse(McpSchemaBuilder.ForQuery(parameters, false, options))!;
    }

    [Fact] void should_use_the_configured_property_name() => _schema["properties"]!["customer_id"].ShouldNotBeNull();
    [Fact] void should_require_the_configured_property_name() => _schema["required"]!.AsArray().Select(_ => _!.GetValue<string>()).ShouldContainOnly("customer_id");
    [Fact] void should_use_the_configured_converter() => _schema["properties"]!["priority"]!["enum"]!.AsArray().Select(_ => _!.GetValue<string>()).ShouldContainOnly("low", "high");
}
