// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Mcp.for_McpEndpoint.given;
using Microsoft.AspNetCore.Builder;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_discovering_combined_with_gated_endpoints_unset : an_mcp_host
{
    McpResponse _metadata;
    McpResponse _mcp;
    McpResponse _otherRoute;

    protected override McpProfile Profile => McpProfile.Combined;
    protected override bool SelectGatedEndpoints => false;

    protected override void ConfigureEndpoints(WebApplication app) => app.MapGet("/other", () => "reached");

    async Task Because()
    {
        Unauthenticate();
        _metadata = await Get("/.well-known/oauth-protected-resource/mcp");
        _mcp = await ListTools();
        _otherRoute = await Get("/other");
    }

    [Fact] void should_serve_the_discovery_document_without_a_caller() => _metadata.Status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_still_refuse_the_mcp_endpoint() => _mcp.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_still_refuse_other_routes_on_the_port() => _otherRoute.Status.ShouldEqual(HttpStatusCode.Unauthorized);
}
