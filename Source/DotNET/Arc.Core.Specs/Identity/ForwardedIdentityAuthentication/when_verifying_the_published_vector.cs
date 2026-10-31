// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_verifying_the_published_vector : given.a_signed_request
{
    ForwardedIdentityAuthenticationResult _result;
    string _canonical;

    void Because()
    {
        var envelope = ForwardedIdentityEnvelope.Parse(ForwardedIdentityForSpecs.Envelope)!;
        _canonical = System.Text.Encoding.UTF8.GetString(ForwardedIdentityCanonicalization.Create(ForwardedIdentitySnapshot.Capture(_context, _options)!, _options, envelope)!.CanonicalBytes);
        _result = Authenticate();
    }

    [Fact] void should_match_the_external_canonical_bytes() => _canonical.ShouldEqual(ForwardedIdentityForSpecs.Canonical);
    [Fact] void should_accept_the_external_mac() => _result.Failure.ShouldBeNull();
    [Fact] void should_authenticate_the_subject() => _result.Principal!.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value.ShouldEqual("u");
    [Fact] void should_preserve_the_name() => _result.Principal!.Identity!.Name.ShouldEqual("n");
    [Fact] void should_preserve_the_provider() => _result.Principal!.FindFirst(MicrosoftIdentityPlatformClaims.IdentityProvider)!.Value.ShouldEqual("aad");
}
