// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_the_host_changes_after_capture : given.a_signed_request
{
    ForwardedIdentityAuthenticationResult _result;
    int _principalReads;
    int _signatureReads;

    void Establish()
    {
        var raw = (ICanReadForwardedIdentityRequest)_context;
        raw.GetHeaderValues(MicrosoftIdentityPlatformHeaders.PrincipalHeader).Returns(_ =>
            ++_principalReads == 1 ? [ForwardedIdentityForSpecs.Principal] : ["forged-after-capture"]);
        raw.GetHeaderValues(ForwardedIdentityForSpecs.SignatureHeader).Returns(_ =>
            ++_signatureReads == 1 ? [ForwardedIdentityForSpecs.Envelope] : ["forged-after-capture"]);
        _headers[MicrosoftIdentityPlatformHeaders.PrincipalHeader] = "flattened-dictionary-is-not-authoritative";
    }

    void Because() => _result = Authenticate();

    [Fact] void should_read_the_principal_once() => _principalReads.ShouldEqual(1);
    [Fact] void should_read_the_signature_once() => _signatureReads.ShouldEqual(1);
    [Fact] void should_authenticate_the_captured_bytes() => _result.Principal.Identity.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_build_identity_from_the_same_snapshot() => _result.Principal.Identity.Name.ShouldEqual("n");
}
