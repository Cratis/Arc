// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_an_unchanged_admitted_hop : given.a_v2_resolver
{
    async Task Because()
    {
        _first = await _resolver.GetCurrent();
        _result = await _resolver.GetCurrent();
    }

    [Fact] void should_accept_the_first_resolution() => _first.Caller.ShouldNotBeNull();
    [Fact] void should_reuse_the_verified_caller() => ReferenceEquals(_first, _result).ShouldBeTrue();
    [Fact] void should_have_no_refusal() => _result.Refusal.ShouldBeNull();
    [Fact] void should_resolve_the_person_once() => _persons.Calls.ShouldEqual(1);
}
