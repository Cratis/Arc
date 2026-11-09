// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_no_caller_is_verified : an_mcp_host
{
    McpResponse _call;
    McpResponse _list;
    McpResponse _stream;
    int _dispatchedWhenRefused;
    McpResponse _control;
    McpResponse _streamWhenVerified;

    async Task Because()
    {
        Unauthenticate();
        _call = await CallTool("write_note", """{"text":"x"}""");
        _list = await ListTools();
        _stream = await Get("/mcp");
        _dispatchedWhenRefused = _probe.Count;

        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        _control = await CallTool("write_note", """{"text":"x"}""");
        _streamWhenVerified = await Get("/mcp");
    }

    [Fact] void should_refuse_a_call_as_unauthorized() => _call.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_challenge_with_the_application_realm() => _call.Challenge.ShouldEqual("Bearer realm=\"app\"");
    [Fact] void should_answer_in_json() => _call.ContentType.ShouldEqual("application/json");
    [Fact] void should_refuse_a_listing_as_unauthorized() => _list.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_refuse_an_event_stream_as_unauthorized() => _stream.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_not_dispatch_anything_refused() => _dispatchedWhenRefused.ShouldEqual(0);
    [Fact] void should_run_the_call_for_a_verified_caller_in_the_same_setup() => (_control.IsToolError, _probe.Count).ShouldEqual((false, 1));
    [Fact] void should_not_offer_an_event_stream_to_a_verified_caller() => _streamWhenVerified.Status.ShouldEqual(HttpStatusCode.MethodNotAllowed);
}
