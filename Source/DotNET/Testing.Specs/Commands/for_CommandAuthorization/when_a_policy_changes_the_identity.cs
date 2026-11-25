// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_a_policy_changes_the_identity : given.an_evaluation
{
    void Establish() => Behavior.Evaluate = async (_, _) =>
    {
        await Task.Yield();
        ((ClaimsIdentity)Principal.Identity).AddClaim(new Claim(ClaimTypes.Role, "Admin"));
        return true;
    };

    async Task Because() => Result = await CommandAuthorization.For(Provider, Principal, new given.ResourceCommand(true));
    [Fact] void should_not_certify_the_changed_identity() => Result.ShouldBeDenied();
    [Fact] void should_restore_the_principal() => Accessor.Current.ShouldBeNull();
}
