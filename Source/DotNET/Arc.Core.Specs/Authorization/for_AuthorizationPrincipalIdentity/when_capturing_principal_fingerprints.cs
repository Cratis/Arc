// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text;

namespace Cratis.Arc.Authorization.for_AuthorizationPrincipalIdentity;

public class when_capturing_principal_fingerprints : Specification
{
    ClaimsPrincipal?[] _principals;
    string[] _legacy;
    string[] _fingerprints;

    void Establish()
    {
        _principals =
        [
            null,
            new ClaimsPrincipal(new ClaimsIdentity()),
            new ClaimsPrincipal(IdentityWithClaims()),
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("æøå<&>😀", "\"æøå<&>😀\n\u2028\u2029")], "æøå<&>😀", "subject", "permission") { Label = "æøå<&>😀" }),
            new ClaimsPrincipal(new ClaimsIdentity { Actor = new ClaimsIdentity([new Claim("actor", "first")]) { Actor = new ClaimsIdentity([new Claim("actor", "second")]) } }),
            new ClaimsPrincipal(new ClaimsIdentity { BootstrapContext = new object() }),
            new ClaimsPrincipal(new ClaimsIdentity { BootstrapContext = "æøå<&>😀" }),
            new ClaimsPrincipal(new given.CustomClaimsIdentity()),
            new ClaimsPrincipal([new ClaimsIdentity(), IdentityWithClaims(), new given.CustomClaimsIdentity()]),
            new ClaimsPrincipal()
        ];
        _legacy = _principals.Select(given.LegacyPrincipalFingerprint.For).ToArray();
    }

    void Because() => _fingerprints = _principals.Select(principal => AuthorizationPrincipalIdentity.Capture(principal).Fingerprint).ToArray();

    [Fact] void should_preserve_a_null_principal() => _fingerprints[0].ShouldEqual(_legacy[0]);
    [Fact] void should_preserve_an_identity_without_claims_and_with_null_values() => _fingerprints[1].ShouldEqual(_legacy[1]);
    [Fact] void should_preserve_claim_order_and_ordinal_property_order() => _fingerprints[2].ShouldEqual(_legacy[2]);
    [Fact] void should_preserve_default_unicode_and_html_escaping() => _fingerprints[3].ShouldEqual(_legacy[3]);
    [Fact] void should_preserve_a_recursive_actor_chain() => _fingerprints[4].ShouldEqual(_legacy[4]);
    [Fact] void should_preserve_an_opaque_bootstrap_context() => _fingerprints[5].ShouldEqual(_legacy[5]);
    [Fact] void should_preserve_a_string_bootstrap_context() => _fingerprints[6].ShouldEqual(_legacy[6]);
    [Fact] void should_preserve_a_custom_identity_type_name() => _fingerprints[7].ShouldEqual(_legacy[7]);
    [Fact] void should_preserve_multiple_identities_in_order() => _fingerprints[8].ShouldEqual(_legacy[8]);
    [Fact] void should_preserve_an_empty_principal() => _fingerprints[9].ShouldEqual(_legacy[9]);
    [Fact] void should_preserve_every_utf8_byte() => _fingerprints.Zip(_legacy).All(pair => Encoding.UTF8.GetBytes(pair.First).SequenceEqual(Encoding.UTF8.GetBytes(pair.Second))).ShouldBeTrue();

    static ClaimsIdentity IdentityWithClaims()
    {
        var claim = new Claim("permission", "edit", "urn:test:value", "issuer", "original-issuer");
        claim.Properties.Add("z", "last");
        claim.Properties.Add("a", "lower");
        claim.Properties.Add("A", "upper");
        claim.Properties.Add("æ", "æøå<&>😀");
        return new ClaimsIdentity([claim, new Claim("permission", "read")], "test", "subject", "permission") { Label = "ordered" };
    }
}
