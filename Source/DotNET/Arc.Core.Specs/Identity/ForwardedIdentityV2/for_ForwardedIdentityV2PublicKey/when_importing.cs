// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Arc.Identity.ForwardedIdentityV2.given;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2PublicKey;

public class when_importing : Specification
{
    readonly List<bool> _refusals = [];
    bool _accepted;

    void Because()
    {
        using var wrongCurve = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        var fixture = ForwardedIdentityV2Vectors.Fixture.TestKeys[0];
        foreach (var (id, pem) in new[] { ("!", fixture.PublicKeyPem), ("", fixture.PublicKeyPem), (new string('a', 65), fixture.PublicKeyPem), ("k1", fixture.PrivateKeyPem), ("k1", wrongCurve.ExportSubjectPublicKeyInfoPem()) })
        {
            try
            {
                _ = ForwardedIdentityV2PublicKey.FromPem(id, pem);
                _refusals.Add(false);
            }
            catch (ArgumentException)
            {
                _refusals.Add(true);
            }
        }
        var vector = ForwardedIdentityV2Vectors.Named("valid-v2");
        var canonical = ForwardedIdentityV2Canonicalization.Create(VerificationFixture.Settings(vector.Inputs), VerificationFixture.Request(vector), vector.Envelope.Split('.'));
        _accepted = ForwardedIdentityV2PublicKey.FromPem("k1", fixture.PublicKeyPem).Verify(canonical.Bytes, ForwardedIdentityV2Envelope.Parse(vector.Envelope).Envelope!.Signature);
    }

    [Fact] void should_reject_invalid_ids_private_material_and_wrong_curves() => _refusals.ShouldEqual(new[] { true, true, true, true, true });
    [Fact] void should_accept_the_public_p256_key() => _accepted.ShouldBeTrue();
}
