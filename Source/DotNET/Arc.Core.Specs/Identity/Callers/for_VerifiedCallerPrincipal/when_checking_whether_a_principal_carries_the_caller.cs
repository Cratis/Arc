// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization.given;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerPrincipal;

public class when_checking_whether_a_principal_carries_the_caller : Specification
{
    VerifiedCaller _caller;
    ClaimsPrincipal _principal;

    void Establish()
    {
        _caller = verified_callers.Person("person-1");
        _principal = VerifiedCallerPrincipal.Create(_caller);
    }

    [Fact] void should_carry_it_when_created_for_it() => VerifiedCallerPrincipal.Carries(_principal, _caller).ShouldBeTrue();

    [Fact] void should_carry_it_through_a_clone() => VerifiedCallerPrincipal.Carries(new ClaimsPrincipal(_principal.Identities.Select(identity => identity.Clone())), _caller).ShouldBeTrue();

    [Fact] void should_not_carry_it_for_a_plain_identity_with_the_same_subject() =>
        VerifiedCallerPrincipal.Carries(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "person-1")], "other")), _caller).ShouldBeFalse();

    [Fact] void should_not_carry_it_for_an_equal_but_different_caller_instance() =>
        VerifiedCallerPrincipal.Carries(VerifiedCallerPrincipal.Create(verified_callers.Person("person-1")), _caller).ShouldBeFalse();

    [Fact] void should_not_carry_it_for_a_null_principal() => VerifiedCallerPrincipal.Carries(null, _caller).ShouldBeFalse();
}
