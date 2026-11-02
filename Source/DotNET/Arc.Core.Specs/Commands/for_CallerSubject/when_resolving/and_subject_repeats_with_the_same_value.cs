// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Commands.for_CallerSubject.when_resolving;

public class and_subject_repeats_with_the_same_value : given.a_caller
{
    void Establish() => ((ClaimsIdentity)_principal.Identity).AddClaim(new Claim("sub", "represented"));
    void Because() => _result = Resolve();

    [Fact] void should_find_one_distinct_subject() => _result.Key.ShouldEqual("represented");
}
