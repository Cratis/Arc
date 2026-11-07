// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

public class with_retired_presented_tokens : given.a_gate
{
    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\f")]
    [InlineData("\v")]
    [InlineData(" \t\r\n\f\v")]
    void should_refuse_every_known_retired_prefix_after_ascii_scheme_whitespace(string separator)
    {
        foreach (var prefix in ReservedCredentialPrefixes.KnownRetiredPrefixes)
        {
            var token = prefix == "dpat" ? $"dpat_{new string('a', 32)}_{new string('b', 64)}" : $"{prefix}_synthetic-opaque";
            foreach (var scheme in new[] { "Bearer", "Basic", "DPoP" })
            {
                _raw.GetHeaderValues("Authorization").Returns([$"{scheme}{separator}{token.ToUpperInvariant()}"]);
                _gate.Inspect(_request).Refusal.ShouldEqual(CallerRefusal.LegacyCredentialUnsupported);
            }
        }
    }

    [Theory]
    [InlineData("drun_synthetic-opaque")]
    [InlineData("dpat_ malformed")]
    void should_refuse_a_retired_token_in_the_custom_envelope_header_without_configuration(string value)
    {
        _raw.HeaderNames.Returns([ForwardedIdentityEnvelope.Header]);
        _raw.GetHeaderValues(ForwardedIdentityEnvelope.Header).Returns([value]);
        _gate.Inspect(_request).Refusal.ShouldEqual(CallerRefusal.LegacyCredentialUnsupported);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Bearer%09")]
    void should_refuse_a_retired_token_in_a_query_value_without_configuration(string scheme)
    {
        _raw.RawTarget.Returns($"/mcp?%61ccess%5Ftoken={scheme}dpat_{new string('a', 32)}_{new string('b', 64)}");
        _gate.Inspect(_request).Refusal.ShouldEqual(CallerRefusal.LegacyCredentialUnsupported);
    }

    [Theory]
    [InlineData("dpatient")]
    [InlineData("druntime")]
    void should_not_treat_a_prefix_without_the_underscore_as_retired(string token)
    {
        _raw.GetHeaderValues("Authorization").Returns(["Bearer " + token]);
        var result = _gate.Inspect(_request);
        result.Refusal.ShouldBeNull();
        result.Shape.ShouldEqual(PresentedCredentialShape.OAuthBearer);
    }
}
