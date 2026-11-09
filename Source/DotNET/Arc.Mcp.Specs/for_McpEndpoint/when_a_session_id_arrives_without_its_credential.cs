// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_a_session_id_arrives_without_its_credential : a_pat_authenticated_host
{
    McpResponse _anonymous;
    McpResponse _other;

    async Task Because()
    {
        await Initialize();
        _anonymous = await SessionCall(null);
        _other = await SessionCall(SyntheticPat());
    }

    [Fact] void should_initialize() => _initialized.Status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_not_treat_the_session_id_as_a_credential() => _anonymous.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_not_reuse_authority_for_another_credential() => _other.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_verify_the_other_credential_against_authority() => _authorityReads.ShouldEqual(2);
    [Fact] void should_not_dispatch() => _probe.Count.ShouldEqual(0);
}
