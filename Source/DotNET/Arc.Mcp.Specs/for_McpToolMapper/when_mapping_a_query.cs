// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.Mcp.for_McpToolCatalog.given;
using Cratis.Arc.Mcp.for_McpToolMapper.given;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Mcp.for_McpToolMapper;

public class when_mapping_a_query : a_mapper_context
{
    McpToolDescriptor _paged;
    McpToolDescriptor _unpaged;
    JsonNode _pagedSchema;
    JsonNode _unpagedSchema;

    void Establish() => AddPerformerFor(nameof(ReadModelWithQueries.Exposed), true, new QueryParameter("Value", typeof(string), true));

    void Because()
    {
        _paged = MapQuery(QueryName(nameof(ReadModelWithQueries.Exposed)), "paged");
        _unpaged = MapQuery(QueryName(nameof(ReadModelWithQueries.Everything)), "unpaged");
        _pagedSchema = JsonNode.Parse(_paged.InputSchema!)!;
        _unpagedSchema = JsonNode.Parse(_unpaged.InputSchema!)!;
    }

    [Fact] void should_be_a_query_tool_for_the_query() => (_paged.Kind, _paged.Operation).ShouldEqual((McpToolKind.Query, new FullyQualifiedQueryName(QueryName(nameof(ReadModelWithQueries.Exposed)))));
    [Fact] void should_be_a_read_tool() => _paged.Access.ShouldEqual(McpToolAccess.Read);
    [Fact] void should_require_only_the_read_scope() => _paged.RequiredScopes.ShouldContainOnly("app:read");
    [Fact] void should_be_read_only() => _paged.Annotations.ReadOnly.ShouldBeTrue();
    [Fact] void should_not_be_destructive() => _paged.Annotations.Destructive.ShouldBeFalse();
    [Fact] void should_be_idempotent() => _paged.Annotations.Idempotent.ShouldBeTrue();
    [Fact] void should_not_accept_an_idempotency_key() => _pagedSchema["properties"]!["idempotency_key"].ShouldBeNull();
    [Fact] void should_offer_paging_when_the_query_supports_it() => _pagedSchema["properties"]![McpSchemaBuilder.PageSizeName].ShouldNotBeNull();
    [Fact] void should_not_offer_paging_when_the_query_does_not_support_it() => _unpagedSchema["properties"]![McpSchemaBuilder.PageSizeName].ShouldBeNull();
    [Fact] void should_describe_the_query_parameters() => _pagedSchema["properties"]!["value"].ShouldNotBeNull();
}
