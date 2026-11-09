// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Arc.Identity.ForwardedIdentityV2.given;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2Vectors;

public class when_checking_the_published_fixture : Specification
{
    Fixture _fixture;
    bool _everyKeyPairMatches;

    void Establish() => _fixture = ForwardedIdentityV2Vectors.Fixture;

    void Because()
    {
        _everyKeyPairMatches = _fixture.TestKeys.All(key =>
        {
            using var privateKey = ECDsa.Create();
            using var publicKey = ECDsa.Create();
            privateKey.ImportFromPem(key.PrivateKeyPem);
            publicKey.ImportFromPem(key.PublicKeyPem);
            var data = "published-fixture-key-check"u8.ToArray();
            var signature = privateKey.SignData(data, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            return publicKey.VerifyData(data, signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        });
    }

    [Fact] void should_label_the_keys_as_test_only() => _fixture.Warning.ToUpperInvariant().ShouldContain("TEST ONLY");
    [Fact] void should_publish_matching_key_pairs() => _everyKeyPairMatches.ShouldBeTrue();
    [Fact] void should_use_the_published_format_name() => _fixture.Format.ShouldEqual("Cratis.AuthProxy.ForwardedIdentity.v2");
}
