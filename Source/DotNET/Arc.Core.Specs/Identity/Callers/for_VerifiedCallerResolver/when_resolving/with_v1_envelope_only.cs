// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_v1_envelope_only : given.a_resolver
{
    void Establish()
    {
        _sources.Clear();
        _raw.HeaderNames.Returns(["x-cratis-forwarded-identity-signature", "x-ms-client-principal"]);
        _raw.GetHeaderValues("Authorization").Returns([]);
        _raw.GetHeaderValues("x-cratis-forwarded-identity-signature").Returns(["v1.synthetic.signature"]);
    }
    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_refuse_without_a_source_of_required_facts() => _result.Refusal.ShouldEqual(CallerRefusal.NoMatchingSource);
    [Fact] void should_not_construct_a_caller() => _result.Caller.ShouldBeNull();
}
