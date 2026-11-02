// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Commands.for_CallerSubject.when_resolving;

public class and_only_the_actor_has_a_subject : given.a_caller
{
    void Establish() => _principal = new ClaimsPrincipal(new ClaimsIdentity([], "test") { Actor = new ClaimsIdentity([new Claim("sub", "actor")], "test") });
    void Because() => _result = Resolve();

    [Fact] void should_be_missing() => _result.Outcome.ShouldEqual(ReadModelKeyLookupOutcome.Missing);
}
