// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Authentication;
using Cratis.Arc.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.given;

public class a_handler : Specification
{
    protected const string IdentityIdFromHeader = "identity-id-from-header";
    protected const string IdentityNameFromHeader = "identity-name-from-header";
    protected const string UserDetails = "user@example.com";
    protected const string BenignClaimType = "email";
    protected const string BenignClaimValue = "user@example.com";
    protected const string IngressProviderKeyClaimType = "urn:cratis:identity:provider-key";
    protected const string IngressProviderKeyClaimValue = "workforce";

    protected MicrosoftIdentityPlatformAuthenticationHandler _handler;

    void Establish()
    {
        var options = Substitute.For<IOptions<ArcOptions>>();
        options.Value.Returns(new ArcOptions());
        _handler = new(options, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Builds a request context carrying the three Microsoft Identity Platform headers, with the principal header
    /// holding the exact base64 encoded JSON wire shape the ingress forwards.
    /// </summary>
    /// <param name="identityProvider">The value of the <c>identityProvider</c> field, or null to omit the field entirely.</param>
    /// <param name="claims">The claims to put in the serialized principal.</param>
    /// <returns>The <see cref="IHttpRequestContext"/> to authenticate.</returns>
    protected static IHttpRequestContext ContextForPrincipal(string? identityProvider, params (string Type, string Value)[] claims)
    {
        var context = Substitute.For<IHttpRequestContext>();
        context.Headers.Returns(new Dictionary<string, string>
        {
            [MicrosoftIdentityPlatformHeaders.IdentityIdHeader] = IdentityIdFromHeader,
            [MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = IdentityNameFromHeader,
            [MicrosoftIdentityPlatformHeaders.PrincipalHeader] = PrincipalHeaderValue(identityProvider, claims)
        });

        return context;
    }

    /// <summary>
    /// Serializes the principal exactly the way the ingress does - camel cased field names, claims as typ/val pairs -
    /// so the specs pin the wire contract rather than whatever Arc's own serializer options happen to produce.
    /// </summary>
    /// <param name="identityProvider">The value of the <c>identityProvider</c> field, or null to omit the field entirely.</param>
    /// <param name="claims">The claims to put in the serialized principal.</param>
    /// <returns>The base64 encoded principal header value.</returns>
    protected static string PrincipalHeaderValue(string? identityProvider, params (string Type, string Value)[] claims)
    {
        var payload = new Dictionary<string, object>
        {
            ["userId"] = "provider-user-id",
            ["userDetails"] = UserDetails,
            ["userRoles"] = new[] { "authenticated" },
            ["claims"] = claims.Select(claim => new { typ = claim.Type, val = claim.Value }).ToArray()
        };

        if (identityProvider is not null)
        {
            payload["identityProvider"] = identityProvider;
        }

        return Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(payload));
    }

    protected static string[] IdentityProviderClaimsFrom(AuthenticationResult result) =>
        [.. result.Principal!.FindAll(MicrosoftIdentityPlatformClaims.IdentityProvider).Select(claim => claim.Value)];

    protected static string[] ClaimValuesFrom(AuthenticationResult result, string claimType) =>
        [.. result.Principal!.FindAll(claimType).Select(claim => claim.Value)];
}
