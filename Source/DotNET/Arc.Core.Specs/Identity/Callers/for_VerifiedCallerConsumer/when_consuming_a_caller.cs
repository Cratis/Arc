// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;
using VerifiedCallerConsumer;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerConsumer;

public class when_consuming_a_caller : Specification
{
    VerifiedCaller _expected;
    VerifiedCaller? _result;
    VerifiedCaller? _denied;

    void Establish() => _expected = new VerifiedCallerBuilder(verified_facts.Catalog).Build(verified_facts.Delegated(), verified_facts.Installation, verified_facts.Tenant).Caller!;

    async Task Because()
    {
        var accessor = Substitute.For<IVerifiedCallerAccessor>();
        accessor.GetCurrent().Returns(new CallerResolution(_expected, null));
        var handler = new ToolHandler(accessor);
        _result = await handler.Handle(caller => caller.PrincipalKind == CallerPrincipalKind.DelegatedActor && caller.Scopes.Contains("fixture:read"));
        _denied = await handler.Handle(_ => false);
    }

    [Fact] void should_read_the_framework_caller() => _result.ShouldEqual(_expected);
    [Fact] void should_expose_the_person_separately() => _result!.Person!.Value.ShouldEqual("person-fixture");
    [Fact] void should_expose_the_checked_actor() => _result!.Actor.ShouldEqual(verified_facts.Actor);
    [Fact] void should_expose_the_checked_grant() => _result!.GrantId!.Value.ShouldEqual("grant-fixture");
    [Fact] void should_expose_the_verified_credential_kind() => _result!.CredentialKind.ShouldEqual(CallerCredentialKind.OAuthGrant);
    [Fact] void should_still_require_product_authorization() => _denied.ShouldBeNull();
}
