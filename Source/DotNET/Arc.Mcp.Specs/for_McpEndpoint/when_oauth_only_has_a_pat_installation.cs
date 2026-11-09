// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_oauth_only_has_a_pat_installation : an_installed_caller_host
{
    protected override McpProfile Profile => McpProfile.OAuthOnly;

    Task Because() => CallPresentations();

    [Fact] void should_refuse_pat() => _pat.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_accept_verified_oauth() => _oauth.Status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_list_tools_for_oauth() => _oauth.Body.ShouldContain("write_note");
    [Fact] void should_refuse_anonymous() => _anonymous.Status.ShouldEqual(HttpStatusCode.Unauthorized);
}
