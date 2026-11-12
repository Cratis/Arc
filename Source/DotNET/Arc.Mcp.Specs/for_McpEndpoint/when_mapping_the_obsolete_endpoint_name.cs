// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;
using Microsoft.AspNetCore.Builder;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_mapping_the_obsolete_endpoint_name : an_mcp_host
{
    McpResponse _anonymous;
    McpResponse _verified;

#pragma warning disable CS0618 // Exercising the obsolete forwarder on purpose.
    protected override void MapMcpEndpoint(WebApplication app) => app.MapCratisArcMcp();
#pragma warning restore CS0618

    async Task Because()
    {
        Unauthenticate();
        _anonymous = await ListTools();
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        _verified = await ListTools();
    }

    [Fact] void should_gate_the_endpoint() => _anonymous.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_serve_a_verified_caller() => _verified.Status.ShouldEqual(HttpStatusCode.OK);
}
