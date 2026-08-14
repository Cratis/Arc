// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIDentityPlatformAuthHandler.when_handling_authentication;

/// <summary>
/// Pins the boundary of the strip, and with it the documented rule that a consumer must read reserved claims with
/// FindFirst/FindAll and never normalize the claim type itself.
/// </summary>
/// <remarks>
/// The handler strips with OrdinalIgnoreCase and FindFirst/FindAll match with OrdinalIgnoreCase, so a claim type
/// those two agree is different both survives the strip and stays invisible to the documented lookup - every
/// assertion that observes through FindAll shares the guard's blind spot by construction. The last two assertions
/// therefore observe with a different comparison, which is the only way this suite can show what the warning in the
/// documentation is about. They describe a known limitation rather than a desirable property: if the strip is ever
/// widened to fold Unicode casing, they fail, and the documentation must be corrected in the same change.
/// </remarks>
public class and_the_colliding_claim_differs_only_by_unicode_case_folding : given.a_handler
{
    const string IdentityProviderFromIngress = "aad";
    const string ForgedIdentityProvider = "forged-by-the-caller";

    /// <summary>
    /// The reserved type with U+017F LATIN SMALL LETTER LONG S in place of the <c>s</c> of <c>cratis</c>.
    /// </summary>
    /// <remarks>
    /// OrdinalIgnoreCase says this is a different claim type, so it is neither stripped nor found by FindAll.
    /// ToUpperInvariant folds it to the same string as the reserved type, so a consumer that upper cases the type
    /// before comparing does match it - and forwarded claims are added before Arc writes its own, so that consumer
    /// meets the forgery first.
    /// </remarks>
    const string LongSClaimType = "urn:cratiſ:arc:identity:provider";

    AuthenticateResult _result;

    async Task Because() => _result = await Authenticate(
        IdentityProviderFromIngress,
        (LongSClaimType, ForgedIdentityProvider),
        (BenignClaimType, BenignClaimValue));

    [Fact] void should_authenticate() => _result.Succeeded.ShouldBeTrue();
    [Fact] void should_keep_the_unrelated_claim() => ClaimValuesFrom(_result, BenignClaimType).ShouldContainOnly(BenignClaimValue);
    [Fact] void should_expose_only_the_ingress_identity_provider_through_the_documented_lookup() => IdentityProviderClaimsFrom(_result).ShouldContainOnly(IdentityProviderFromIngress);
    [Fact] void should_leave_the_unicode_variant_as_a_claim_type_of_its_own() => ClaimValuesFrom(_result, LongSClaimType).ShouldContainOnly(ForgedIdentityProvider);
    [Fact] void should_match_both_claims_for_a_consumer_that_upper_cases_the_claim_type() => ClaimValuesNormalizedWithUpperInvariant(_result).Length.ShouldEqual(2);
    [Fact] void should_put_the_forgery_first_for_a_consumer_that_upper_cases_the_claim_type() => ClaimValuesNormalizedWithUpperInvariant(_result)[0].ShouldEqual(ForgedIdentityProvider);
}
