// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_an_unsigned_envelope : given.a_resolver
{
    void Establish()
    {
        _raw.HeaderNames.Returns(["x-ms-client-principal", "x-cratis-identity", "x-cratis-envelope-version"]);
        _raw.GetHeaderValues("Authorization").Returns([]);
    }
    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_require_a_signature_presentation() => _result.Refusal.ShouldEqual(CallerRefusal.NoCredential);
    [Fact] void should_not_construct_a_caller() => _result.Caller.ShouldBeNull();
}
