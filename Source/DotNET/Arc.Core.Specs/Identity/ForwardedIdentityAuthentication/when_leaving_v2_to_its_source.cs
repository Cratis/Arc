// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_leaving_v2_to_its_source : given.a_signed_request
{
    AuthenticationResult _v2;
    AuthenticationResult _unsigned;
    AuthenticationResult _disabled;

    async Task Because()
    {
        var handler = new MicrosoftIdentityPlatformAuthenticationHandler(Options.Create(_options), NullLoggerFactory.Instance);
        _headers[ForwardedIdentityForSpecs.SignatureHeader] = "v2.not-a-v1-envelope";
        _v2 = await handler.HandleAuthentication(_context);
        _options.ForwardedIdentity.Enabled = false;
        _options.ForwardedIdentityV2 = true;
        _headers.Remove(ForwardedIdentityForSpecs.SignatureHeader);
        _unsigned = await handler.HandleAuthentication(_context);
        _options.ForwardedIdentity.Enabled = true;
        _options.TrustForwardedIdentityHeaders = false;
        _disabled = await handler.HandleAuthentication(_context);
    }

    [Fact] void should_not_report_a_v1_parse_failure_for_v2() => _v2.ShouldEqual(AuthenticationResult.Anonymous);
    [Fact] void should_disable_unsigned_v1_when_v2_is_configured() => _unsigned.ShouldEqual(AuthenticationResult.Anonymous);
    [Fact] void should_ignore_signed_forwarding_with_the_master_switch_off() => _disabled.ShouldEqual(AuthenticationResult.Anonymous);
}
