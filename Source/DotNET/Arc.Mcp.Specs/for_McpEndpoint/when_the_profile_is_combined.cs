// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_the_profile_is_combined : a_profile_matrix
{
    protected override McpProfile Profile => McpProfile.Combined;

    protected override IReadOnlySet<CallerCredentialKind> Accepted { get; } = new HashSet<CallerCredentialKind>
    {
        CallerCredentialKind.PersonalAccessToken,
        CallerCredentialKind.OAuthGrant
    };

    Task Because() => CallEveryKind();

    [Fact] void should_admit_personal_access_tokens_and_oauth_grants() => AdmittedKinds().ShouldBeTrue();
    [Fact] void should_refuse_every_other_kind_as_unauthorized_without_running_the_tool() => RefusedKinds().ShouldBeTrue();
    [Fact] void should_have_checked_every_kind() => CheckedEveryKind().ShouldBeTrue();
}
