// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization.given;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerIdentity;

public class when_cloning : Specification
{
    VerifiedCaller _caller;
    VerifiedCallerIdentity _original;
    System.Security.Claims.ClaimsIdentity _clone;

    void Establish()
    {
        _caller = verified_callers.Person("person-1", "a:read");
        _original = new VerifiedCallerIdentity(_caller);
    }

    void Because() => _clone = _original.Clone();

    [Fact] void should_be_a_verified_caller_identity() => _clone.ShouldBeOfExactType<VerifiedCallerIdentity>();

    [Fact] void should_keep_the_same_caller_reference() => ((VerifiedCallerIdentity)_clone).Caller.ShouldEqual(_caller);

    [Fact] void should_keep_the_claims() => _clone.Claims.Count().ShouldEqual(_original.Claims.Count());
}
