// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Primitives;

namespace Cratis.Arc.Identity.for_MicrosoftIDentityPlatformAuthHandler.given;

public class an_on_behalf_forward : a_handler
{
    protected const string RepresentedRole = "customer-admin";
    protected const string ActorId = "support-person-id";
    protected const string ActorDetails = "support@cratis.io";
    protected const string ActorRole = "support";
    protected const string Tenant = "tenant-42";

    protected static readonly string RepresentedPrincipalHeaderValue = ClientPrincipalHeaderValue(IdentityIdFromHeader, UserDetails, RepresentedRole);

    void Establish() => _arcOptions.OnBehalfOf = OnBehalfOfAssertionsForSpecs.Options();

    /// <summary>
    /// Authenticates an on-behalf forward signed the way AuthProxy signs it: the represented user in the Microsoft Identity
    /// Platform headers, the actor in <see cref="OnBehalfOfHeaders.ActorHeader"/> and the assertion binding both in
    /// <see cref="OnBehalfOfHeaders.AssertionHeader"/>.
    /// </summary>
    /// <param name="actorHeader">The raw actor header value.</param>
    /// <returns>The <see cref="AuthenticateResult"/> from the handler.</returns>
    protected Task<AuthenticateResult> AuthenticateOnBehalfOf(string actorHeader) =>
        AuthenticateOnBehalfOf(actorHeader, OnBehalfOfAssertionsForSpecs.Sign(ClaimsFor(actorHeader)));

    /// <summary>
    /// Authenticates an on-behalf forward with exact actor and assertion header values.
    /// </summary>
    /// <param name="actorHeader">The actor header values.</param>
    /// <param name="assertion">The assertion header values.</param>
    /// <returns>The <see cref="AuthenticateResult"/> from the handler.</returns>
    protected Task<AuthenticateResult> AuthenticateOnBehalfOf(StringValues actorHeader, StringValues assertion) =>
        Authenticate(new Dictionary<string, StringValues>
        {
            [MicrosoftIdentityPlatformHeaders.IdentityIdHeader] = IdentityIdFromHeader,
            [MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = IdentityNameFromHeader,
            [MicrosoftIdentityPlatformHeaders.PrincipalHeader] = RepresentedPrincipalHeaderValue,
            [Tenancy.Constants.DefaultTenantIdHeader] = Tenant,
            [OnBehalfOfHeaders.ActorHeader] = actorHeader,
            [OnBehalfOfHeaders.AssertionHeader] = assertion
        });

    /// <summary>
    /// Creates the assertion claims AuthProxy issues for a forward of the given actor header.
    /// </summary>
    /// <param name="actorHeader">The raw actor header value.</param>
    /// <returns>The claims.</returns>
    protected static Dictionary<string, object?> ClaimsFor(string actorHeader)
    {
        var claims = OnBehalfOfAssertionsForSpecs.ClaimsFor(RepresentedPrincipalHeaderValue, actorHeader, IdentityIdFromHeader, ActorIdFrom(actorHeader));
        claims["tenant"] = Tenant;
        return claims;
    }

    /// <summary>
    /// Serializes the actor exactly the way AuthProxy does - the same client principal wire shape as the principal header.
    /// </summary>
    /// <param name="userId">The actor user id.</param>
    /// <returns>The base64 encoded actor header value.</returns>
    protected static string ActorHeaderValue(string userId = ActorId) => ClientPrincipalHeaderValue(userId, ActorDetails, ActorRole);

    static string ClientPrincipalHeaderValue(string userId, string userDetails, string role) =>
        Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new
        {
            identityProvider = "aad",
            userId,
            userDetails,
            userRoles = new[] { role },
            claims = new[] { new { typ = BenignClaimType, val = BenignClaimValue } }
        }));

    static string ActorIdFrom(string actorHeader)
    {
        try
        {
            using var document = JsonDocument.Parse(Convert.FromBase64String(actorHeader));
            var userId = document.RootElement.TryGetProperty("userId", out var value) ? value.GetString() : null;
            return string.IsNullOrEmpty(userId) ? ActorId : userId;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return ActorId;
        }
    }
}
