// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_poisoning_a_nonce_with_an_invalid_mac : given.a_signed_request
{
    ForwardedIdentityAuthenticationResult _invalid;
    ForwardedIdentityAuthenticationResult _valid;

    void Because()
    {
        _rawHeaders[ForwardedIdentityForSpecs.SignatureHeader] = [ForwardedIdentityForSpecs.Envelope[..^1] + "s"];
        _invalid = Authenticate();
        _rawHeaders[ForwardedIdentityForSpecs.SignatureHeader] = [ForwardedIdentityForSpecs.Envelope];
        _valid = Authenticate();
    }

    [Fact] void should_reject_the_invalid_mac() => _invalid.Principal.ShouldBeNull();
    [Fact] void should_not_consume_the_nonce() => _valid.Principal.ShouldNotBeNull();
}
