// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_the_payload_is_oversized : an_mcp_host
{
    McpResponse _oversized;
    int _dispatchedWhenRefused;
    McpResponse _control;

    protected override long MaxRequestBodySize => 1024;

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        _oversized = await CallTool("write_note", $$"""{"text":"{{new string('a', 2048)}}"}""");
        _dispatchedWhenRefused = _probe.Count;
        _control = await CallTool("write_note", """{"text":"x"}""");
    }

    [Fact] void should_refuse_with_content_too_large() => _oversized.Status.ShouldEqual(HttpStatusCode.RequestEntityTooLarge);
    [Fact] void should_name_the_refusal() => _oversized.RefusalError.ShouldEqual("request_too_large");
    [Fact] void should_not_dispatch_the_oversized_call() => _dispatchedWhenRefused.ShouldEqual(0);
    [Fact] void should_run_a_payload_within_the_limit_in_the_same_setup() => (_control.IsToolError, _probe.Count).ShouldEqual((false, 1));
}
