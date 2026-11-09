// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_pat_only_has_a_pat_installation : an_installed_caller_host
{
    Task Because() => CallPresentations();

    [Fact] void should_accept_pat() => _pat.Status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_refuse_oauth() => _oauth.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_refuse_anonymous() => _anonymous.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_not_dispatch() => _probe.Count.ShouldEqual(0);
}
