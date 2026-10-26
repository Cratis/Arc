// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIDentityPlatformAuthHandler.given;

public class an_on_behalf_forward : a_handler
{
    protected const string RepresentedRole = "customer-admin";
    protected const string ActorId = "support-person-id";
    protected const string ActorDetails = "support@cratis.io";
    protected const string ActorRole = "support";

    /// <summary>
    /// Authenticates an on-behalf forward: the represented user in the Microsoft Identity Platform headers and the
    /// actor in <see cref="OnBehalfOfHeaders.ActorHeader"/>.
    /// </summary>
    /// <param name="actorHeader">The raw actor header value.</param>
    /// <returns>The <see cref="AuthenticateResult"/> from the handler.</returns>
    protected Task<AuthenticateResult> AuthenticateOnBehalfOf(string actorHeader) =>
        Authenticate(new Dictionary<string, string>
        {
            [MicrosoftIdentityPlatformHeaders.IdentityIdHeader] = IdentityIdFromHeader,
            [MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = IdentityNameFromHeader,
            [MicrosoftIdentityPlatformHeaders.PrincipalHeader] = ClientPrincipalHeaderValue("provider-user-id", UserDetails, RepresentedRole),
            [OnBehalfOfHeaders.ActorHeader] = actorHeader
        });

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
}
