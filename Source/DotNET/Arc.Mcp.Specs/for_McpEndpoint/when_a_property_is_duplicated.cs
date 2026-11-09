// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_a_property_is_duplicated : an_mcp_host
{
    McpResponse _duplicatedToolName;
    McpResponse _duplicatedArgument;
    McpResponse _duplicatedMethod;
    int _dispatchedWhenRefused;
    McpResponse _control;

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope);
        _duplicatedToolName = await Post("""{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"who_am_i","name":"write_note","arguments":{"text":"x"}}}""");
        _duplicatedArgument = await Post("""{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"who_am_i","arguments":{"tenant":"a","tenant":"b"}}}""");
        _duplicatedMethod = await Post("""{"jsonrpc":"2.0","id":1,"method":"tools/list","method":"tools/call","params":{"name":"who_am_i","arguments":{}}}""");
        _dispatchedWhenRefused = _probe.Count;
        _control = await CallTool("who_am_i", "{}");
    }

    [Fact] void should_refuse_a_duplicated_tool_name() => _duplicatedToolName.Status.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_refuse_a_duplicated_argument() => _duplicatedArgument.Status.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_refuse_a_duplicated_method() => _duplicatedMethod.Status.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_name_the_refusal() => _duplicatedToolName.RefusalError.ShouldEqual("invalid_request");
    [Fact] void should_not_dispatch_anything_refused() => _dispatchedWhenRefused.ShouldEqual(0);
    [Fact] void should_run_a_well_formed_call_in_the_same_setup() => (_control.IsToolError, _probe.Count).ShouldEqual((false, 1));
}
