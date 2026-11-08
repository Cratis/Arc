// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_the_caller_lacks_the_write_scope : an_mcp_host
{
    McpResponse _refused;
    McpResponse _listedForReader;
    int _dispatchedWhenRefused;
    McpResponse _listedForWriter;
    McpResponse _control;

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope);
        _refused = await CallTool("write_note", """{"text":"x"}""");
        _listedForReader = await ListTools();
        _dispatchedWhenRefused = _probe.Count;

        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        _listedForWriter = await ListTools();
        _control = await CallTool("write_note", """{"text":"x"}""");
    }

    [Fact] void should_refuse_as_forbidden() => _refused.Status.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_name_the_refusal() => _refused.RefusalError.ShouldEqual("insufficient_scope");
    [Fact] void should_challenge_for_the_missing_scope() => _refused.Challenge.ShouldEqual("Bearer error=\"insufficient_scope\", scope=\"app:write\"");
    [Fact] void should_not_point_a_token_caller_to_oauth_metadata() => _refused.Challenge.ShouldNotContain("resource_metadata");
    [Fact] void should_not_dispatch_the_refused_call() => _dispatchedWhenRefused.ShouldEqual(0);
    [Fact] void should_list_only_the_tools_the_reader_can_call() => _listedForReader.ToolNames.ShouldContainOnly("who_am_i");
    [Fact] void should_list_every_tool_for_a_writer_in_the_same_setup() => _listedForWriter.ToolNames.ShouldContainOnly("who_am_i", "write_note");
    [Fact] void should_run_the_call_for_a_writer_in_the_same_setup() => (_control.IsToolError, _probe.Count).ShouldEqual((false, 1));
}
