// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_combined_has_a_pat_installation : an_installed_caller_host
{
    protected override McpProfile Profile => McpProfile.Combined;

    Task Because() => CallPresentations();

    [Fact] void should_accept_pat() => _pat.Status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_accept_verified_oauth() => _oauth.Status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_list_tools_for_oauth() => _oauth.Body.ShouldContain("write_note");
    [Fact] void should_refuse_anonymous() => _anonymous.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_still_accept_pat_on_other_gated_endpoints() => _patEndpoint.Status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_still_refuse_oauth_on_other_gated_endpoints() => _oauthEndpoint.Status.ShouldEqual(HttpStatusCode.Unauthorized);
}
