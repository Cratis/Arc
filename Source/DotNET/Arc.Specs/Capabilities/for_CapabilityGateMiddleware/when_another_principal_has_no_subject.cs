// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Capabilities.for_CapabilityGateMiddleware;

public class when_another_principal_has_no_subject : given.a_scheme_that_authenticates_a_subject
{
    protected override IReadOnlyList<Claim>? OtherClaims() => [new Claim(ClaimTypes.Role, "admin")];

    void Establish() => _verified.GetCurrent().Returns(new CallerResolution(_person, null));

    Task Because() => Invoke();

    [Fact] void should_refuse_as_forbidden_because_the_match_cannot_be_proved() => _context.Response.StatusCode.ShouldEqual(403);

    [Fact] void should_not_run_handler() => _nextRan.ShouldBeFalse();
}
