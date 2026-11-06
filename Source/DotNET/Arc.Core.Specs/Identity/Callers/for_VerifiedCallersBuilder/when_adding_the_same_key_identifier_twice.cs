// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.ForwardedIdentityV2;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallersBuilder;

public class when_adding_the_same_key_identifier_twice : given.an_installation
{
    void Because() => _exception = Catch.Exception(() => new SignedForwardedIdentityV2Builder().AddPublicKey("test", _publicKey).AddPublicKey("test", _publicKey));
    [Fact] void should_refuse_the_duplicate() => _exception.ShouldBeOfExactType<ArgumentException>();
}
