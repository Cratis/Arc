// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_authentication;

/// <summary>
/// A blank but present claim is worse for a consumer than an absent one - a null check passes and the value is
/// meaningless - so a forwarded field holding nothing but whitespace must produce no claim at all, exactly as an
/// empty or omitted field does.
/// </summary>
public class and_the_identity_provider_is_blank : given.a_handler
{
    const string EmptyIdentityProvider = "";
    const string SpacesOnlyIdentityProvider = "   ";
    const string TabAndNewlineIdentityProvider = "\t\n";

    AuthenticationResult _empty;
    AuthenticationResult _spacesOnly;
    AuthenticationResult _tabAndNewline;

    async Task Because()
    {
        _empty = await _handler.HandleAuthentication(ContextForPrincipal(EmptyIdentityProvider, (BenignClaimType, BenignClaimValue)));
        _spacesOnly = await _handler.HandleAuthentication(ContextForPrincipal(SpacesOnlyIdentityProvider, (BenignClaimType, BenignClaimValue)));
        _tabAndNewline = await _handler.HandleAuthentication(ContextForPrincipal(TabAndNewlineIdentityProvider, (BenignClaimType, BenignClaimValue)));
    }

    [Fact] void should_authenticate_the_principal_with_an_empty_identity_provider() => _empty.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_authenticate_the_principal_with_a_spaces_only_identity_provider() => _spacesOnly.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_authenticate_the_principal_with_a_tab_and_newline_identity_provider() => _tabAndNewline.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_not_expose_an_identity_provider_for_the_empty_field() => IdentityProviderClaimsFrom(_empty).ShouldBeEmpty();
    [Fact] void should_not_expose_an_identity_provider_for_the_spaces_only_field() => IdentityProviderClaimsFrom(_spacesOnly).ShouldBeEmpty();
    [Fact] void should_not_expose_an_identity_provider_for_the_tab_and_newline_field() => IdentityProviderClaimsFrom(_tabAndNewline).ShouldBeEmpty();
    [Fact] void should_keep_the_unrelated_claim() => ClaimValuesFrom(_spacesOnly, BenignClaimType).ShouldContainOnly(BenignClaimValue);
}
