// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Capabilities.for_CapabilityGateMiddleware;

public class when_another_scheme_authenticated_the_same_subject : given.a_scheme_that_authenticates_a_subject
{
    protected override IReadOnlyList<Claim>? OtherClaims() => [new Claim("sub", "person")];

    void Establish() => _verified.GetCurrent().Returns(new CallerResolution(_person, null));

    Task Because() => Invoke();

    [Fact] void should_run_handler() => _nextRan.ShouldBeTrue();

    [Fact] void should_use_the_verified_principal() => VerifiedCallerPrincipal.Carries(_context.User, _person).ShouldBeTrue();
}
