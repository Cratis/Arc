// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_a_tool_returns_a_rich_result : an_mcp_host
{
    JsonElement _listedTool;
    JsonElement _result;
    McpResponse _missingStructure;
    McpResponse _arrayStructure;
    McpResponse _relativeLink;
    McpResponse _nullLinks;

    protected override void ConfigureMcp(McpBuilder mcp) => mcp.AddToolSource<RichTool>();

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope);
        var listed = await ListTools();
        _listedTool = listed.Message.GetProperty("result").GetProperty("tools").EnumerateArray().Single(_ => _.GetProperty("name").GetString() == "count_notes");
        _result = (await CallTool("count_notes", "{}")).Message.GetProperty("result");
        _missingStructure = await CallTool("missing_structure", "{}");
        _arrayStructure = await CallTool("array_structure", "{}");
        _relativeLink = await CallTool("relative_link", "{}");
        _nullLinks = await CallTool("null_links", "{}");
    }

    [Fact] void should_list_the_title() => _listedTool.GetProperty("title").GetString().ShouldEqual("Count notes");
    [Fact] void should_list_the_output_schema() => _listedTool.GetProperty("outputSchema").GetProperty("type").GetString().ShouldEqual("object");
    [Fact] void should_return_the_structured_content() => _result.GetProperty("structuredContent").GetProperty("count").GetInt32().ShouldEqual(2);
    [Fact] void should_return_the_text_first() => _result.GetProperty("content")[0].GetProperty("text").GetString().ShouldEqual("""{"count":2}""");
    [Fact] void should_return_the_resource_link() => _result.GetProperty("content")[1].GetProperty("type").GetString().ShouldEqual("resource_link");
    [Fact] void should_return_the_resource_link_uri() => _result.GetProperty("content")[1].GetProperty("uri").GetString().ShouldEqual("notes://notes/1");
    [Fact] void should_return_the_resource_link_name() => _result.GetProperty("content")[1].GetProperty("name").GetString().ShouldEqual("First note");
    [Fact] void should_return_the_resource_link_mime_type() => _result.GetProperty("content")[1].GetProperty("mimeType").GetString().ShouldEqual("text/plain");
    [Fact] void should_fail_a_tool_that_declares_a_schema_but_returns_no_structure() => (_missingStructure.IsToolError, _missingStructure.ToolText).ShouldEqual((true, McpToolDispatcher.Failed));
    [Fact] void should_fail_a_structure_that_is_not_an_object() => (_arrayStructure.IsToolError, _arrayStructure.ToolText).ShouldEqual((true, McpToolDispatcher.Failed));
    [Fact] void should_treat_null_links_as_none() => (_nullLinks.IsToolError, _nullLinks.ToolText, _nullLinks.Message.GetProperty("result").GetProperty("content").GetArrayLength()).ShouldEqual((false, "no links", 1));
    [Fact] void should_fail_a_link_without_an_absolute_uri() => (_relativeLink.IsToolError, _relativeLink.ToolText).ShouldEqual((true, McpToolDispatcher.Failed));
}
