// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Identity.for_CausingIdentity;

public class when_mapping_from_a_principal : Specification
{
    CausingIdentity _authenticated;
    CausingIdentity _unauthenticated;
    CausingIdentity _none;
    CausingIdentity _system;
    CausingIdentity _forgedSystem;
    CausingIdentity _forgedAnonymous;
    CausingIdentity _acting;

    void Because()
    {
        _authenticated = CausingIdentity.From(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "s"), new Claim("name", "n"), new Claim("preferred_username", "u")], "test")));
        _unauthenticated = CausingIdentity.From(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "s")])));
        _none = CausingIdentity.From(null);
        _system = CausingIdentity.From(SystemPrincipal.WithRoles());
        _forgedSystem = CausingIdentity.From(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", SystemPrincipal.Subject)], "other")));
        _forgedAnonymous = CausingIdentity.From(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "[Anonymous]")], "other")));
        var represented = new ClaimsIdentity([new Claim("sub", "rep"), new Claim("name", "Rep")], "test")
        {
            Actor = new ClaimsIdentity([new Claim("sub", "act"), new Claim("name", "Act")], "test")
        };
        _acting = CausingIdentity.From(new ClaimsPrincipal(represented));
    }

    [Fact] void should_map_the_claims() => (_authenticated.Subject.Value, _authenticated.Name.Value, _authenticated.UserName.Value).ShouldEqual(("s", "n", "u"));
    [Fact] void should_not_flag_an_authenticated_caller() => (_authenticated.IsAnonymous, _authenticated.IsSystem).ShouldEqual((false, false));
    [Fact] void should_be_anonymous_when_unauthenticated() => _unauthenticated.ShouldEqual(CausingIdentity.Anonymous);
    [Fact] void should_be_anonymous_without_a_principal() => _none.ShouldEqual(CausingIdentity.Anonymous);
    [Fact] void should_flag_anonymous() => _none.IsAnonymous.ShouldBeTrue();
    [Fact] void should_map_the_system_principal() => _system.ShouldEqual(CausingIdentity.System);
    [Fact] void should_flag_system() => _system.IsSystem.ShouldBeTrue();
    [Fact] void should_not_treat_a_forged_system_subject_as_system() => _forgedSystem.IsSystem.ShouldBeFalse();
    [Fact] void should_not_equal_system_for_a_forged_subject() => _forgedSystem.ShouldNotEqual(CausingIdentity.System);
    [Fact] void should_not_treat_a_forged_anonymous_subject_as_anonymous() => _forgedAnonymous.IsAnonymous.ShouldBeFalse();
    [Fact] void should_not_equal_anonymous_for_a_forged_subject() => _forgedAnonymous.ShouldNotEqual(CausingIdentity.Anonymous);
    [Fact] void should_use_the_actor_as_the_caller() => _acting.Subject.Value.ShouldEqual("act");
    [Fact] void should_carry_the_represented_identity() => _acting.OnBehalfOf!.Subject.Value.ShouldEqual("rep");
}
