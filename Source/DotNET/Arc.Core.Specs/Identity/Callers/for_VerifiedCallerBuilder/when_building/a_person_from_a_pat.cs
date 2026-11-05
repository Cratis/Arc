// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

/// <summary>Vector 0.</summary>
public class a_person_from_a_pat : given.a_builder
{
    void Because() => Build();

    [Fact] void should_build_a_person() => _result.Caller.PrincipalKind.ShouldEqual(CallerPrincipalKind.Person);
    [Fact] void should_keep_the_verified_credential() => _result.Caller.CredentialId.ShouldEqual(_facts.CredentialId);
    [Fact] void should_keep_the_account() => _result.Caller.AccountKey.ShouldEqual(_facts.AccountKey);
    [Fact] void should_keep_the_person() => _result.Caller.Person.ShouldEqual(_facts.Person);
    [Fact] void should_not_invent_an_actor() => _result.Caller.Actor.ShouldBeNull();
    [Fact] void should_set_the_local_version() => _result.Caller.ContractVersion.ShouldEqual("1");
    [Fact] void should_keep_the_verified_scope_ceiling() => _result.Caller.Scopes.ShouldContain("fixture:read");
}
