// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Features;

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_an_unrelated_verifier_uses_a_later_clock : given.a_signed_handler
{
    AuthenticateResult _result;

    async Task Because()
    {
        (await Authenticate()).Succeeded.ShouldBeTrue();
        var original = _context;
        ResetRequest();
        var later = DateTimeOffset.FromUnixTimeSeconds(1700000061);
        _clock.GetUtcNow().Returns(later);
        _headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(_headers, timestamp: later.ToUnixTimeSeconds());
        ApplyHeaders();
        ForwardedIdentityAuthentication.Authenticate(new AspNetCoreHttpRequestContext(_context), _options, "unrelated", "/base/a%2Fb", _clock).Principal.ShouldNotBeNull();
        _context = original;
        _clock.GetUtcNow().Returns(DateTimeOffset.FromUnixTimeSeconds(1700000000));
        _context.Features.Get<IHttpRequestFeature>()!.RawTarget = "/base/a%2fb?x=2";
        _result = await Authenticate();
    }

    [Fact] void should_preserve_the_isolated_nonce_reservation_when_the_complete_snapshot_changes() => _result.Succeeded.ShouldBeFalse();
}
