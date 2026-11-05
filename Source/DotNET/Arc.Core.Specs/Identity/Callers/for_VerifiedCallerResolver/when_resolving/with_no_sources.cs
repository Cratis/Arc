// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_no_sources : given.a_resolver
{
    void Establish() => _sources.Clear();
    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_refuse_plain_bearer_without_a_verifier() => _result.Refusal.ShouldEqual(CallerRefusal.NoMatchingSource);
    [Fact] void should_not_construct_a_caller() => _result.Caller.ShouldBeNull();
}
