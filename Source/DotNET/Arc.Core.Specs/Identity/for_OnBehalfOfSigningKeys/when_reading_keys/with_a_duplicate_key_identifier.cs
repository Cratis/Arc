// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_OnBehalfOfSigningKeys.when_reading_keys;

public class with_a_duplicate_key_identifier : Specification
{
    OnBehalfOfOptions _options;
    OnBehalfOfSigningKeys _keys;

    void Establish()
    {
        _options = OnBehalfOfAssertionsForSpecs.Options();
        _options.SigningKeys.Add(new() { KeyId = OnBehalfOfAssertionsForSpecs.KeyId, PublicKeyPem = _options.SigningKeys[0].PublicKeyPem });
    }

    void Because() => _keys = OnBehalfOfSigningKeys.For(_options);

    [Fact] void should_refuse_the_configuration() => _keys.Problem.ShouldNotBeNull();
    [Fact] void should_say_why() => _keys.Problem.ShouldContain("more than once");
}
