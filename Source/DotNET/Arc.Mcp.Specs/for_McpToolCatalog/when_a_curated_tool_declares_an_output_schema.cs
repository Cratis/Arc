// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCatalog.given;

namespace Cratis.Arc.Mcp.for_McpToolCatalog;

public class when_a_curated_tool_declares_an_output_schema : a_catalog_context
{
    Exception? _notJson;
    Exception? _notAnObject;
    Exception? _arraySchema;
    McpToolCatalog _valid;

    void Because()
    {
        _notJson = Catch.Exception(() => Catalog("{not json"));
        _notAnObject = Catch.Exception(() => Catalog("[]"));
        _arraySchema = Catch.Exception(() => Catalog("""{"type":"array"}"""));
        _valid = Catalog("""{"type":"object"}""");
    }

    [Fact] void should_refuse_a_schema_that_is_not_json() => _notJson.ShouldBeOfExactType<McpToolOutputSchemaInvalid>();
    [Fact] void should_refuse_a_schema_that_is_not_an_object() => _notAnObject.ShouldBeOfExactType<McpToolOutputSchemaInvalid>();
    [Fact] void should_refuse_a_schema_that_does_not_describe_an_object() => _arraySchema.ShouldBeOfExactType<McpToolOutputSchemaInvalid>();
    [Fact] void should_keep_a_valid_schema() => _valid.Tools.Single().OutputSchema.ShouldEqual("""{"type":"object"}""");
    [Fact] void should_keep_the_title() => _valid.Tools.Single().Title.ShouldEqual("A title");

    McpToolCatalog Catalog(string schema) =>
        McpToolCatalog.Build(_exposure, _options, _commands, _queries, [new Source(schema)]);

    sealed class Source(string schema) : IMcpToolSource
    {
        public IEnumerable<McpToolDescriptor> Describe() =>
        [
            McpToolDescriptor.Custom("curated_tool", "A curated tool.", ["app:read"], McpToolAccess.Read) with { Title = "A title", OutputSchema = schema }
        ];

        public ValueTask<McpToolResult> Invoke(McpToolInvocation invocation) => ValueTask.FromResult(McpToolResult.Success("ok"));
    }
}
