// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_sending_a_batch : an_mcp_host
{
    McpResponse _batch;
    McpResponse _malformed;
    int _dispatchedWhenRefused;
    McpResponse _control;

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        _batch = await Post("""[{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"who_am_i","arguments":{}}},{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"write_note","arguments":{"text":"x"}}}]""");
        _malformed = await Post("""{"jsonrpc":"2.0","id":1,"method":"tools/call",""");
        _dispatchedWhenRefused = _probe.Count;
        _control = await CallTool("who_am_i", "{}");
    }

    [Fact] void should_refuse_the_batch() => _batch.Status.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_name_the_refusal() => _batch.RefusalError.ShouldEqual("invalid_request");
    [Fact] void should_refuse_malformed_json() => _malformed.Status.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_not_dispatch_anything_refused() => _dispatchedWhenRefused.ShouldEqual(0);
    [Fact] void should_run_a_single_call_in_the_same_setup() => (_control.IsToolError, _probe.Count).ShouldEqual((false, 1));
}
