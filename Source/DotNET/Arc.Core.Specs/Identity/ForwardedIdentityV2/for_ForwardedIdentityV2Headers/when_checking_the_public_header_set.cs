// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.ForwardedIdentityV2.given;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2Headers;

public class when_checking_the_public_header_set : Specification
{
    string[] _vectorHeaderNames;
    string[] _missingFromPublicSet;
    bool _signatureIncluded;
    bool _signatureMatchesEnvelope;
    bool _tenantHeaderIncluded;
    bool _tenantHeaderIncludedWithoutConfiguration;
    bool _unrelatedIncluded;
    bool _prefixFamiliesIncluded;
    bool _everyExactNameCovered;
    string[] _documentedNamesMissing;
    bool _signatureExcludedFromCoverage;

    static readonly string[] _documentedNames =
    [
        "x-cratis-token-client-id", "x-cratis-token-scope", "x-cratis-tenant-id", "tenant-id", "x-cratis-microservice", "service-id",
        "x-cratis-credential-kind", "x-cratis-credential-id", "x-cratis-issuer", "x-cratis-subject", "x-cratis-idp", "x-cratis-auth-time",
        "x-cratis-acr", "x-cratis-amr", "x-cratis-act", "x-cratis-grant-id", "x-cratis-policy-epoch", "x-cratis-sender-binding", "x-cratis-forwarded-token-sha256"
    ];

    void Because()
    {
        var vector = ForwardedIdentityV2Vectors.Named("valid-v2");
        _vectorHeaderNames = VerificationFixture.Request(vector).Headers.Select(header => header.Name).ToArray();
        _missingFromPublicSet = _vectorHeaderNames
            .Where(name => ForwardedIdentityV2Canonicalization.IsCovered(name, null) && !ForwardedIdentityV2Headers.Includes(name))
            .ToArray();
        _signatureIncluded = ForwardedIdentityV2Headers.Includes(ForwardedIdentityV2Headers.SignatureHeader.ToUpperInvariant());
        _signatureMatchesEnvelope = ForwardedIdentityV2Headers.SignatureHeader == ForwardedIdentityV2Envelope.Header;
        _tenantHeaderIncluded = ForwardedIdentityV2Headers.Includes("X-Tenant", "x-tenant");
        _tenantHeaderIncludedWithoutConfiguration = ForwardedIdentityV2Headers.Includes("x-tenant");
        _unrelatedIncluded = ForwardedIdentityV2Headers.Includes("authorization") || ForwardedIdentityV2Headers.Includes("cookie");
        _prefixFamiliesIncluded = ForwardedIdentityV2Headers.Includes("X-MS-Client-Principal-Extra") && ForwardedIdentityV2Headers.Includes("x-cratis-on-behalf-extra");
        _everyExactNameCovered = ForwardedIdentityV2Headers.CoveredHeaders.All(name => ForwardedIdentityV2Canonicalization.IsCovered(name, null));
        _documentedNamesMissing = _documentedNames.Where(name => !ForwardedIdentityV2Headers.Includes(name.ToUpperInvariant())).ToArray();
        _signatureExcludedFromCoverage = !ForwardedIdentityV2Headers.IsCovered(ForwardedIdentityV2Headers.SignatureHeader, ForwardedIdentityV2Headers.SignatureHeader);
    }

    [Fact] void should_include_every_documented_name_without_regard_to_case() => _documentedNamesMissing.ShouldBeEmpty();
    [Fact] void should_not_cover_the_signature_header_even_when_it_is_the_tenant_header() => _signatureExcludedFromCoverage.ShouldBeTrue();
    [Fact] void should_have_vector_headers() => _vectorHeaderNames.ShouldNotBeEmpty();
    [Fact] void should_include_every_header_the_canonicalization_covers() => _missingFromPublicSet.ShouldBeEmpty();
    [Fact] void should_include_the_signature_header_without_regard_to_case() => _signatureIncluded.ShouldBeTrue();
    [Fact] void should_use_the_name_the_envelope_verifies() => _signatureMatchesEnvelope.ShouldBeTrue();
    [Fact] void should_include_the_configured_tenant_header() => _tenantHeaderIncluded.ShouldBeTrue();
    [Fact] void should_not_include_an_unconfigured_tenant_header() => _tenantHeaderIncludedWithoutConfiguration.ShouldBeFalse();
    [Fact] void should_not_include_unrelated_headers() => _unrelatedIncluded.ShouldBeFalse();
    [Fact] void should_include_the_prefix_families() => _prefixFamiliesIncluded.ShouldBeTrue();
    [Fact] void should_cover_every_exact_name_in_the_canonicalization() => _everyExactNameCovered.ShouldBeTrue();
}
