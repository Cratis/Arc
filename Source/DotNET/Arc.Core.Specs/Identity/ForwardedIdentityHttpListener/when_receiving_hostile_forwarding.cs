// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Identity.for_ForwardedIdentityHttpListener;

public class when_receiving_hostile_forwarding : given.a_listener
{
    AuthenticationResult _valid;
    AuthenticationResult _forged;
    AuthenticationResult _wrongKey;
    AuthenticationResult _disabled;
    AuthenticationResult _unsigned;

    async Task Because()
    {
        var headers = ForwardedIdentityForSpecs.Headers();
        headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(headers);
        _valid = await Authenticate(headers, reenter: true);
        headers[MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = "forged";
        _forged = await Authenticate(headers);
        headers = ForwardedIdentityForSpecs.Headers();
        headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(headers, key: new byte[32]);
        _wrongKey = await Authenticate(headers);
        headers[ForwardedIdentityForSpecs.SignatureHeader] = "intentionally-invalid";
        _disabled = await Authenticate(headers, enabled: false);
        headers.Remove(ForwardedIdentityForSpecs.SignatureHeader);
        _unsigned = await Authenticate(headers);
    }

    [Fact] void should_verify_the_real_listener_raw_target() => _valid.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_reenter_on_the_same_listener_request() => _reentered.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_reject_forged_identity() => _forged.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_reject_the_wrong_key() => _wrongKey.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_preserve_legacy_behavior_while_disabled() => _disabled.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_not_fall_back_to_unsigned_identity() => _unsigned.IsAuthenticated.ShouldBeFalse();
}
