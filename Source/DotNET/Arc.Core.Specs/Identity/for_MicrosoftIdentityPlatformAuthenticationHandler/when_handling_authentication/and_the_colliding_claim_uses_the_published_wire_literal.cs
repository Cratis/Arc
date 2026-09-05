// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_authentication;

public class and_the_colliding_claim_uses_the_published_wire_literal : given.a_handler
{
    const string IdentityProviderFromIngress = "aad";
    const string ForgedIdentityProvider = "forged-by-the-caller";

    /// <summary>
    /// Spelled out here instead of taken from <see cref="MicrosoftIdentityPlatformClaims"/> on purpose.
    /// </summary>
    /// <remarks>
    /// That constant is a <c>public const</c>, so it is inlined into every consumer at compile time - this literal,
    /// not the constant, is what the documentation publishes and what an already built consumer keeps looking for.
    /// Every other assertion in this suite routes through the constant, so without the arm below a rename of it
    /// would leave the whole suite green while turning the published literal into an unstripped, caller controlled
    /// claim type carried on an authenticated principal.
    /// </remarks>
    const string PublishedClaimType = "urn:cratis:arc:identity:provider";

    AuthenticationResult _result;

    async Task Because() => _result = await _handler.HandleAuthentication(
        ContextForPrincipal(
            IdentityProviderFromIngress,
            (PublishedClaimType, ForgedIdentityProvider),
            (BenignClaimType, BenignClaimValue)));

    [Fact] void should_publish_the_documented_wire_claim_type() => MicrosoftIdentityPlatformClaims.IdentityProvider.ShouldEqual("urn:cratis:arc:identity:provider");
    [Fact] void should_authenticate() => _result.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_keep_the_unrelated_claim() => ClaimValuesFrom(_result, BenignClaimType).ShouldContainOnly(BenignClaimValue);
    [Fact] void should_strip_the_forgery_from_the_published_claim_type() => ClaimValuesFrom(_result, PublishedClaimType).ShouldNotContain(ForgedIdentityProvider);
    [Fact] void should_expose_the_ingress_identity_provider_under_the_published_claim_type() => ClaimValuesFrom(_result, PublishedClaimType).ShouldContainOnly(IdentityProviderFromIngress);
}
