// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_the_gate_refuses_through_the_mcp_writer : an_mcp_host
{
    McpResponse _badRequest;
    McpResponse _forbidden;

    async Task Because()
    {
        Refuse(CallerRefusal.MultipleCredentials);
        _badRequest = await CallTool("write_note", """{"text":"x"}""");
        Refuse(CallerRefusal.AmbiguousPrincipal);
        _forbidden = await CallTool("write_note", """{"text":"x"}""");
    }

    [Fact] void should_refuse_multiple_credentials_as_a_bad_request() => _badRequest.Status.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_name_the_bad_request() => _badRequest.RefusalError.ShouldEqual("invalid_request");
    [Fact] void should_refuse_an_ambiguous_principal_as_forbidden() => _forbidden.Status.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_name_the_forbidden_refusal() => _forbidden.RefusalError.ShouldEqual("forbidden");
    [Fact] void should_not_dispatch_a_refused_call() => _probe.Count.ShouldEqual(0);
}
