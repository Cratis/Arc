// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Capabilities;
using Cratis.Arc.Mcp.for_McpEndpoint.given;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_the_exemption_is_applied_to_other_routes : an_mcp_host
{
    const string MetadataRoute = "/.well-known/oauth-protected-resource/mcp";

    McpResponse _metadata;
    McpResponse _otherRoute;
    McpResponse _groupedRoute;
    McpResponse _postRoute;

    protected override McpProfile Profile => McpProfile.OAuthOnly;
    protected override bool SelectGatedEndpoints => false;

    protected override void ConfigureEndpoints(WebApplication app)
    {
        // Another route, and every route of a group, carrying an exemption issued for the metadata route.
        app.MapGet("/other", () => "reached").WithMetadata(new CapabilityGateExemption(MetadataRoute));
        app.MapGroup("/grouped").WithMetadata(new CapabilityGateExemption(MetadataRoute)).MapGet("/inner", () => "reached");

        // The right path but not GET-only.
        app.MapPost("/posted", () => "reached").WithMetadata(new CapabilityGateExemption("/posted"));
    }

    async Task Because()
    {
        Unauthenticate();
        _metadata = await Get(MetadataRoute);
        _otherRoute = await Get("/other");
        _groupedRoute = await Get("/grouped/inner");
        _postRoute = await Get("/posted");
    }

    [Fact] void should_still_serve_the_metadata_route() => _metadata.Status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_gate_another_route_carrying_the_exemption() => _otherRoute.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_gate_a_route_group_carrying_the_exemption() => _groupedRoute.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_gate_a_route_that_is_not_get_only() => _postRoute.Status.ShouldEqual(HttpStatusCode.Unauthorized);
}
