// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Capabilities.for_CapabilityGateMiddleware;

public class when_another_scheme_authenticated_a_different_subject : given.a_scheme_that_authenticates_a_subject
{
    protected override IReadOnlyList<Claim>? OtherClaims() => [new Claim("sub", "other")];

    void Establish() => _verified.GetCurrent().Returns(new CallerResolution(_person, null));

    Task Because() => Invoke();

    [Fact] void should_refuse_as_forbidden() => _context.Response.StatusCode.ShouldEqual(403);

    [Fact] void should_answer_problem_json() => _context.Response.ContentType.ShouldEqual("application/problem+json");

    [Fact] void should_not_run_handler() => _nextRan.ShouldBeFalse();

    [Fact] void should_log_the_ambiguous_principal() => _logged.ShouldContain("Capability caller refused: AmbiguousPrincipal");

    [Fact] void should_not_publish_a_principal() => _context.Items.ContainsKey(VerifiedCallerPrincipal.ItemKey).ShouldBeFalse();
}
