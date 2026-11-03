// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_observing_duplicate_header_values : given.a_signed_handler
{
    AuthenticateResult _sameCase;
    AuthenticateResult _differentCase;
    AuthenticateResult _comma;
    int _observedCount;

    async Task Because()
    {
        _context.Request.Headers[MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = new StringValues(["n", "n"]);
        _sameCase = await Authenticate();
        ResetRequest();
        _context.Request.Headers.Append("X-MS-CLIENT-PRINCIPAL-NAME", "n");
        _observedCount = _context.Request.Headers[MicrosoftIdentityPlatformHeaders.IdentityNameHeader].Count;
        _differentCase = await Authenticate();
        ResetRequest();
        _headers[MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = "Last, First";
        _headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(_headers, timestamp: 1700000000);
        ApplyHeaders();
        _comma = await Authenticate();
    }

    [Fact] void should_reject_multiple_observed_values() => _sameCase.Succeeded.ShouldBeFalse();
    [Fact] void should_observe_header_names_case_insensitively() => _observedCount.ShouldEqual(2);
    [Fact] void should_reject_case_variant_duplicates() => _differentCase.Succeeded.ShouldBeFalse();
    [Fact] void should_preserve_a_comma_in_a_single_value() => _comma.Succeeded.ShouldBeTrue();
}
