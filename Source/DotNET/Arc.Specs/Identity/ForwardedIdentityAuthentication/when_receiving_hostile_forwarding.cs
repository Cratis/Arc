// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Primitives;

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_receiving_hostile_forwarding : given.a_signed_handler
{
    AuthenticateResult _forged;
    AuthenticateResult _expired;
    AuthenticateResult _wrongKey;
    AuthenticateResult _replayed;
    AuthenticateResult _duplicate;
    AuthenticateResult _disabled;
    AuthenticateResult _unsigned;

    async Task Because()
    {
        _context.Request.Headers[MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = "forged";
        _forged = await Authenticate();
        ResetRequest();
        _headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(_headers, timestamp: 1699999930);
        ApplyHeaders();
        _expired = await Authenticate();
        ResetRequest();
        _headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(_headers, timestamp: 1700000000, key: new byte[32]);
        ApplyHeaders();
        _wrongKey = await Authenticate();
        ResetRequest();
        (await Authenticate()).Succeeded.ShouldBeTrue();
        _context.Items = new Dictionary<object, object?>();
        _replayed = await Authenticate();
        ResetRequest();
        _context.Request.Headers[MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = new StringValues(["n", "n"]);
        _duplicate = await Authenticate();
        ResetRequest();
        _options.ForwardedIdentity.Enabled = false;
        _context.Request.Headers[ForwardedIdentityForSpecs.SignatureHeader] = "intentionally-invalid";
        _disabled = await Authenticate();
        ResetRequest();
        _context.Request.Headers.Remove(ForwardedIdentityForSpecs.SignatureHeader);
        _unsigned = await Authenticate();
    }

    [Fact] void should_reject_forged_identity() => _forged.Succeeded.ShouldBeFalse();
    [Fact] void should_reject_expired_identity() => _expired.Succeeded.ShouldBeFalse();
    [Fact] void should_reject_the_wrong_key() => _wrongKey.Succeeded.ShouldBeFalse();
    [Fact] void should_reject_a_distinct_request_replay() => _replayed.Succeeded.ShouldBeFalse();
    [Fact] void should_reject_duplicate_raw_fields() => _duplicate.Succeeded.ShouldBeFalse();
    [Fact] void should_preserve_legacy_behavior_when_disabled() => _disabled.Succeeded.ShouldBeTrue();
    [Fact] void should_not_fall_back_to_unsigned_forwarding() => _unsigned.Succeeded.ShouldBeFalse();
}
