// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;
using Microsoft.AspNetCore.Builder;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_gated_endpoints_are_not_configured : an_mcp_host
{
    McpResponse _anonymousCall;
    McpResponse _anonymousList;
    McpResponse _otherRouteAnonymous;
    McpResponse _otherRouteVerified;
    int _dispatchedWhenRefused;
    McpResponse _verifiedCall;

    protected override bool SelectGatedEndpoints => false;

    protected override void ConfigureEndpoints(WebApplication app) => app.MapGet("/other", () => "reached");

    async Task Because()
    {
        Unauthenticate();
        _anonymousCall = await CallTool("write_note", """{"text":"x"}""");
        _anonymousList = await ListTools();
        _otherRouteAnonymous = await Get("/other");
        _dispatchedWhenRefused = _probe.Count;

        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        _verifiedCall = await CallTool("write_note", """{"text":"x"}""");
        _otherRouteVerified = await Get("/other");
    }

    [Fact] void should_refuse_an_anonymous_call_as_unauthorized() => _anonymousCall.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_refuse_in_the_mcp_shape() => (_anonymousCall.Challenge, _anonymousCall.ContentType).ShouldEqual(("Bearer realm=\"app\"", "application/json"));
    [Fact] void should_refuse_an_anonymous_listing() => _anonymousList.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_not_dispatch_anything_refused() => _dispatchedWhenRefused.ShouldEqual(0);
    [Fact] void should_run_the_call_for_a_verified_caller() => (_verifiedCall.IsToolError, _probe.Count).ShouldEqual((false, 1));
    [Fact] void should_still_gate_every_other_route_on_the_capability_port() => _otherRouteAnonymous.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_serve_another_route_to_a_verified_caller() => _otherRouteVerified.Status.ShouldEqual(HttpStatusCode.OK);
}
