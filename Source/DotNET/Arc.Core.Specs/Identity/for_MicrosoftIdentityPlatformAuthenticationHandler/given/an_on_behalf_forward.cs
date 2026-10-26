// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.given;

public class an_on_behalf_forward : a_handler
{
    protected const string RepresentedRole = "customer-admin";
    protected const string ActorId = "support-person-id";
    protected const string ActorDetails = "support@cratis.io";
    protected const string ActorRole = "support";

    /// <summary>
    /// Builds a request context for an on-behalf forward: the represented user in the Microsoft Identity Platform
    /// headers and the actor in <see cref="OnBehalfOfHeaders.ActorHeader"/>.
    /// </summary>
    /// <param name="actorHeader">The raw actor header value.</param>
    /// <returns>The <see cref="IHttpRequestContext"/> to authenticate.</returns>
    protected static IHttpRequestContext ContextForOnBehalfOf(string actorHeader)
    {
        var context = Substitute.For<IHttpRequestContext>();
        context.Items.Returns(new Dictionary<object, object?>());
        context.Headers.Returns(new Dictionary<string, string>
        {
            [MicrosoftIdentityPlatformHeaders.IdentityIdHeader] = IdentityIdFromHeader,
            [MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = IdentityNameFromHeader,
            [MicrosoftIdentityPlatformHeaders.PrincipalHeader] = RepresentedPrincipalHeaderValue(),
            [OnBehalfOfHeaders.ActorHeader] = actorHeader
        });

        return context;
    }

    /// <summary>
    /// Serializes the actor exactly the way AuthProxy does - the same client principal wire shape as the principal header.
    /// </summary>
    /// <param name="userId">The actor user id.</param>
    /// <returns>The base64 encoded actor header value.</returns>
    protected static string ActorHeaderValue(string userId = ActorId) =>
        Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new
        {
            identityProvider = "support-idp",
            userId,
            userDetails = ActorDetails,
            userRoles = new[] { ActorRole },
            claims = new[] { new { typ = "name", val = "Support Person" } }
        }));

    static string RepresentedPrincipalHeaderValue() =>
        Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new
        {
            identityProvider = "aad",
            userId = "provider-user-id",
            userDetails = UserDetails,
            userRoles = new[] { RepresentedRole },
            claims = new[] { new { typ = BenignClaimType, val = BenignClaimValue } }
        }));
}
