// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization.given;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerIdentity;

public class when_creating_for_a_person : Specification
{
    VerifiedCallerIdentity _identity;

    void Because() => _identity = new VerifiedCallerIdentity(verified_callers.Person("person-1", "b:write", "a:read"));

    [Fact] void should_carry_exactly_the_derived_claims() =>
        string.Join(';', _identity.Claims.Select(claim => $"{claim.Type}={claim.Value}")).ShouldEqual(
            "sub=person-1;cratis:credential_kind=BrowserSession;cratis:credential_id=credential;scope=a:read;scope=b:write");

    [Fact] void should_name_the_subject() => _identity.Name.ShouldEqual("person-1");

    [Fact] void should_use_the_scheme_as_authentication_type() => _identity.AuthenticationType.ShouldEqual(VerifiedCallerIdentity.AuthenticationScheme);

    [Fact] void should_be_authenticated() => _identity.IsAuthenticated.ShouldBeTrue();
}
