// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Commands.for_CallerSubject.when_resolving;

public class and_caller_has_two_distinct_subjects : given.a_caller
{
    void Establish() => ((ClaimsIdentity)_principal.Identity).AddClaim(new Claim("sub", "another"));
    void Because() => _result = Resolve();

    [Fact] void should_be_ambiguous() => _result.Outcome.ShouldEqual(ReadModelKeyLookupOutcome.Ambiguous);
}
