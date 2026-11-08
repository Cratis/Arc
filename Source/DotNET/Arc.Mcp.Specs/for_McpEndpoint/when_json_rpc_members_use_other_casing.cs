// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_json_rpc_members_use_other_casing : an_mcp_host
{
    McpResponse _toolNameInOtherCase;
    McpResponse _argumentsInOtherCase;
    McpResponse _smuggledSecondName;
    McpResponse _control;

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope);
        _toolNameInOtherCase = await Post("""{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"NAME":"write_note","ARGUMENTS":{"text":"x"}}}""");
        _argumentsInOtherCase = await Post("""{"JSONRPC":"2.0","ID":1,"METHOD":"tools/call","PARAMS":{"name":"write_note","arguments":{"TEXT":"x"}}}""");
        _smuggledSecondName = await Post("""{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"who_am_i","Name":"write_note","arguments":{"text":"x"}}}""");
        _control = await CallTool("who_am_i", "{}");
    }

    [Fact] void should_not_dispatch_a_write_through_a_differently_cased_name() => IsRefused(_toolNameInOtherCase).ShouldBeTrue();
    [Fact] void should_not_dispatch_a_write_through_differently_cased_members() => IsRefused(_argumentsInOtherCase).ShouldBeTrue();
    [Fact] void should_not_dispatch_a_write_through_a_second_name() => IsRefused(_smuggledSecondName).ShouldBeTrue();
    [Fact] void should_not_run_any_write() => _probe.Dispatched.Count(_ => _.Operation == nameof(WriteNote)).ShouldEqual(0);
    [Fact] void should_still_serve_a_correct_request() => _control.IsToolError.ShouldBeFalse();

    static bool IsRefused(McpResponse response) =>
        response.Status != HttpStatusCode.OK || response.IsProtocolError || response.IsToolError;
}
