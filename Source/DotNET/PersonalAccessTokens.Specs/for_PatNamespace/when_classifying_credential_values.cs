// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens.for_PatNamespace;

public class when_classifying_credential_values : Specification
{
    [Theory]
    [InlineData("direct_pat_")]
    [InlineData("DIRECT_PAT_")]
    [InlineData("DiReCt_pAt_v1_broken")]
    [InlineData("direct_pat_v2_broken")]
    [InlineData("unknown_pat_v1_broken")]
    [InlineData("direct-_pat_v1_broken")]
    [InlineData("stage-atlas_pat_v1_broken")]
    [InlineData("STAGE-ATLAS_PAT_v2_broken")]
    [InlineData("0_pat_")]
    void should_keep_every_pat_namespace_value_out_of_oauth_even_when_format_is_invalid(string value)
    {
        PatNamespace.Classify(value).ShouldEqual(PresentedCredentialShape.PatBearer);
        PatFormat.TryParse(value, out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("eyJhbGciOiJSUzI1NiJ9.payload.signature")]
    [InlineData("ordinary-opaque-oauth-token")]
    [InlineData("_pat_")]
    [InlineData("-direct_pat_")]
    [InlineData("dir_ect_pat_")]
    [InlineData(" direct_pat_")]
    [InlineData("dіrect_pat_")]
    [InlineData("K_pat_")]
    [InlineData("direct_pAt")]
    [InlineData("other.direct_pat_")]
    void should_leave_values_outside_the_anchored_ascii_namespace_for_oauth(string? value) =>
        PatNamespace.Classify(value).ShouldEqual(PresentedCredentialShape.OAuthBearer);

    [Fact]
    void should_classify_a_locked_canonical_token() => PatNamespace.Classify(FormatVectors.Token("direct")).ShouldEqual(PresentedCredentialShape.PatBearer);

    [Theory]
    [InlineData("Bearer")]
    [InlineData("bearer")]
    [InlineData("BEARER")]
    [InlineData("Basic")]
    [InlineData("DPoP")]
    void should_classify_the_extracted_value_independently_of_the_authorization_scheme(string scheme)
    {
        var authorization = $"{scheme} {FormatVectors.Token("direct")}";
        var value = authorization[(authorization.IndexOf(' ', StringComparison.Ordinal) + 1)..];
        PatNamespace.Classify(value).ShouldEqual(PresentedCredentialShape.PatBearer);
    }
}
