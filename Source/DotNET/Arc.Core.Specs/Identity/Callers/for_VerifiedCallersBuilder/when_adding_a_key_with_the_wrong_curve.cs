// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Arc.Identity.ForwardedIdentityV2;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallersBuilder;

public class when_adding_a_key_with_the_wrong_curve : given.an_installation
{
    void Because()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        _exception = Catch.Exception(() => new SignedForwardedIdentityV2Builder().AddPublicKey("test", key.ExportSubjectPublicKeyInfoPem()));
    }
    [Fact] void should_refuse_the_curve() => _exception.ShouldBeOfExactType<InvalidForwardedIdentityPublicKey>();
}
