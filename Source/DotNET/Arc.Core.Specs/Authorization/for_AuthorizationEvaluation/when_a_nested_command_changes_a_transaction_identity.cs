// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

public class when_a_nested_command_changes_a_transaction_identity : Specification
{
    Exception? _differentActor;
    bool _sameIdentityJoined;

    void Because()
    {
        var outer = Principal("actor", "tenant-A", "Default");
        using (AuthorizationCommandIdentity.Enter(outer))
        {
            AuthorizationCommandIdentity.MarkTransactional();
            using (AuthorizationCommandIdentity.Enter(Principal("actor", "tenant-A", "Default")))
            {
                _sameIdentityJoined = true;
            }

            _differentActor = Catch.Exception(() => AuthorizationCommandIdentity.Enter(Principal("other", "tenant-A", "Special")));
        }
    }

    [Fact] void should_allow_the_same_complete_identity_to_join() => _sameIdentityJoined.ShouldBeTrue();
    [Fact] void should_reject_a_different_identity_even_in_the_same_tenant() => _differentActor.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();

    static ClaimsPrincipal Principal(string name, string tenant, string scheme) => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.Name, name), new Claim("tenant_id", tenant)], scheme));
}
