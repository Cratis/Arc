// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_calling_query_tools : an_mcp_host
{
    McpResponse _all;
    McpResponse _filtered;
    McpResponse _pageWithoutSize;
    McpResponse _objectArgument;
    McpResponse _arrayArgument;
    McpResponse _unknownArgument;
    McpResponse _listed;
    McpResponse _streamed;

    protected override void ConfigureMcp(McpBuilder mcp) =>
        mcp.ExposeQuery($"{typeof(NoteReadModel).FullName}.{nameof(NoteReadModel.All)}", "list_notes", "Lists notes.")
            .ExposeQuery($"{typeof(NoteReadModel).FullName}.{nameof(NoteReadModel.Stream)}", "stream_notes", "Streams notes.");

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope);
        _listed = await ListTools();
        _all = await CallTool("list_notes", "{}");
        _filtered = await CallTool("list_notes", """{"prefix":"b"}""");
        _pageWithoutSize = await CallTool("list_notes", """{"page":2}""");
        _objectArgument = await CallTool("list_notes", """{"prefix":{"startsWith":"b"}}""");
        _arrayArgument = await CallTool("list_notes", """{"prefix":["a","b"]}""");
        _streamed = await CallTool("stream_notes", "{}");
        _unknownArgument = await CallTool("list_notes", """{"tenant":"tenant-b"}""");
    }

    [Fact] void should_list_the_query_as_a_read_tool() => _listed.ToolNames.ShouldContain("list_notes");
    [Fact] void should_return_every_row() => (_all.IsToolError, _all.ToolText.Contains("alpha"), _all.ToolText.Contains("gamma")).ShouldEqual((false, true, true));
    [Fact] void should_pass_the_argument_to_the_query() => (_filtered.ToolText.Contains("beta"), _filtered.ToolText.Contains("alpha")).ShouldEqual((true, false));
    [Fact] void should_run_the_query_as_the_verified_caller() => _probe.Dispatched.First().Caller!.Tenant!.Value.ShouldEqual("tenant-a");
    [Fact] void should_not_page_when_only_a_page_is_given() => (_pageWithoutSize.IsToolError, _pageWithoutSize.ToolText.Contains("alpha"), _pageWithoutSize.ToolText.Contains("gamma")).ShouldEqual((false, true, true));
    [Fact] void should_refuse_an_object_argument() => _objectArgument.IsToolError.ShouldBeTrue();
    [Fact] void should_refuse_an_array_for_a_single_value_argument() => _arrayArgument.IsToolError.ShouldBeTrue();
    [Fact] void should_refuse_an_argument_the_query_does_not_declare() => _unknownArgument.IsToolError.ShouldBeTrue();
    [Fact] void should_refuse_a_streaming_query_instead_of_serving_it_without_emission_identity() => (_streamed.IsToolError, _streamed.ToolText.Contains("streamed")).ShouldEqual((true, false));
    [Fact] void should_not_run_a_refused_call() => _probe.Dispatched.Count(_ => _.Operation == "list_notes").ShouldEqual(3);
}
