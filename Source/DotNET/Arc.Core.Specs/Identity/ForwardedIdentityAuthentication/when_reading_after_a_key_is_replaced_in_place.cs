// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_reading_after_a_key_is_replaced_in_place : Specification
{
    ArcOptions _options;
    ForwardedIdentityConfiguration _before;
    ForwardedIdentityConfiguration _after;

    void Establish() => _options = ForwardedIdentityForSpecs.Options();

    void Because()
    {
        _before = ForwardedIdentityConfiguration.Read(_options);
        _options.ForwardedIdentity.Keys[0] = new() { KeyId = "k1", SecretBase64 = Convert.ToBase64String(new byte[32]) };
        _after = ForwardedIdentityConfiguration.Read(_options);
    }

    [Fact] void should_return_a_new_configuration() => _after.ShouldNotBeSame(_before);
    [Fact] void should_use_the_new_key() => _after.Keys["k1"].ShouldNotEqual(_before.Keys["k1"]);
}
