// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.Mcp.for_McpSchemaBuilder.given;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Mcp.for_McpSchemaBuilder;

public class when_building_the_schema_of_a_query : Specification
{
    JsonNode _paged;
    JsonNode _unpaged;
    JsonNode _golden;

    void Because()
    {
        QueryParameter[] parameters = [new("CustomerId", typeof(CustomerId), true), new("Priority", typeof(Priority)), new("Name", typeof(string))];
        _paged = JsonNode.Parse(McpSchemaBuilder.ForQuery(parameters, true))!;
        _unpaged = JsonNode.Parse(McpSchemaBuilder.ForQuery(parameters, false))!;
        _golden = JsonNode.Parse(Goldens.PagedQuery)!;
    }

    [Fact] void should_match_the_golden_schema() => JsonNode.DeepEquals(_paged, _golden).ShouldBeTrue();
    [Fact] void should_offer_paging_inputs_when_paging_is_supported() => (_paged["properties"]!["page"] is not null && _paged["properties"]!["page_size"] is not null).ShouldBeTrue();
    [Fact] void should_not_offer_paging_inputs_when_paging_is_not_supported() => (_unpaged["properties"]!["page"] is null && _unpaged["properties"]!["page_size"] is null).ShouldBeTrue();
    [Fact] void should_name_inputs_as_the_wire_does() => _unpaged["properties"]!["customerId"].ShouldNotBeNull();
    [Fact] void should_reject_unknown_inputs() => _unpaged["additionalProperties"]!.GetValue<bool>().ShouldBeFalse();
}
