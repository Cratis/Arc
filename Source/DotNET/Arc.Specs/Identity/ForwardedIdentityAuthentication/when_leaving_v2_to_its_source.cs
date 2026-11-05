// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_leaving_v2_to_its_source : given.a_signed_handler
{
    AuthenticateResult _v2;
    AuthenticateResult _unsigned;
    AuthenticateResult _disabled;

    async Task Because()
    {
        _headers[ForwardedIdentityForSpecs.SignatureHeader] = "v2.not-a-v1-envelope";
        ApplyHeaders();
        _v2 = await Authenticate();
        _options.ForwardedIdentity.Enabled = false;
        _options.ForwardedIdentityV2 = true;
        _headers.Remove(ForwardedIdentityForSpecs.SignatureHeader);
        ApplyHeaders();
        _unsigned = await Authenticate();
        _options.ForwardedIdentity.Enabled = true;
        _options.TrustForwardedIdentityHeaders = false;
        _disabled = await Authenticate();
    }

    [Fact] void should_not_report_a_v1_parse_failure_for_v2() => _v2.None.ShouldBeTrue();
    [Fact] void should_disable_unsigned_v1_when_v2_is_configured() => _unsigned.None.ShouldBeTrue();
    [Fact] void should_ignore_signed_forwarding_with_the_master_switch_off() => _disabled.None.ShouldBeTrue();
}
