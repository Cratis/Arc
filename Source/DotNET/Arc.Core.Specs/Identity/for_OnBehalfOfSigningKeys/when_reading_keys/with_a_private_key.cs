// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Arc.Identity.for_OnBehalfOfSigningKeys.when_reading_keys;

public class with_a_private_key : Specification
{
    OnBehalfOfOptions _options;
    OnBehalfOfSigningKeys _keys;

    void Establish()
    {
        _options = OnBehalfOfAssertionsForSpecs.Options();
        using var rsa = RSA.Create(2048);
        _options.SigningKeys[0].PublicKeyPem = rsa.ExportRSAPrivateKeyPem();
    }

    void Because() => _keys = OnBehalfOfSigningKeys.For(_options);

    [Fact] void should_refuse_the_configuration() => _keys.Problem.ShouldNotBeNull();
    [Fact] void should_say_why() => _keys.Problem.ShouldContain("private key");
}
