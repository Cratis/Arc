// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.given;

public class an_on_behalf_forward : a_handler
{
    protected const string RepresentedRole = "customer-admin";
    protected const string ActorId = "support-person-id";
    protected const string ActorDetails = "support@cratis.io";
    protected const string ActorRole = "support";
    protected const string Tenant = "tenant-42";
    protected const string Method = "POST";
    protected const string Host = "app.example.com";
    protected const string Path = "/api/tickets/close";

    protected static readonly string RepresentedPrincipalHeaderValue =
        Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new
        {
            identityProvider = "aad",
            userId = IdentityIdFromHeader,
            userDetails = UserDetails,
            userRoles = new[] { RepresentedRole },
            claims = new[] { new { typ = BenignClaimType, val = BenignClaimValue } }
        }));

    protected ArcOptions _arcOptions;

    void Establish()
    {
        _arcOptions = new() { TrustForwardedIdentityHeaders = true, OnBehalfOf = OnBehalfOfAssertionsForSpecs.Options() };
        _handler = new(Options.Create(_arcOptions), NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Builds a request context for an on-behalf forward signed the way AuthProxy signs it: the represented user in the
    /// Microsoft Identity Platform headers, the actor in <see cref="OnBehalfOfHeaders.ActorHeader"/> and the assertion
    /// binding both in <see cref="OnBehalfOfHeaders.AssertionHeader"/>.
    /// </summary>
    /// <param name="actorHeader">The raw actor header value.</param>
    /// <param name="customize">Changes the assertion claims before signing.</param>
    /// <returns>The <see cref="IHttpRequestContext"/> to authenticate.</returns>
    protected static IHttpRequestContext ContextForOnBehalfOf(string actorHeader, Action<Dictionary<string, object?>>? customize = null)
    {
        var claims = ClaimsFor(actorHeader);
        customize?.Invoke(claims);
        return ContextForOnBehalfOf(actorHeader, OnBehalfOfAssertionsForSpecs.Sign(claims), Tenant);
    }

    /// <summary>
    /// Builds a request context for an on-behalf forward with an exact assertion header.
    /// </summary>
    /// <param name="actorHeader">The raw actor header value, or null to leave the header out.</param>
    /// <param name="assertion">The raw assertion header value, or null to leave the header out.</param>
    /// <param name="tenant">The tenant header value, or null to leave the header out.</param>
    /// <returns>The <see cref="IHttpRequestContext"/> to authenticate.</returns>
    protected static IHttpRequestContext ContextForOnBehalfOf(string? actorHeader, string? assertion, string? tenant = null)
    {
        var headers = new Dictionary<string, string>
        {
            [MicrosoftIdentityPlatformHeaders.IdentityIdHeader] = IdentityIdFromHeader,
            [MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = IdentityNameFromHeader,
            [MicrosoftIdentityPlatformHeaders.PrincipalHeader] = RepresentedPrincipalHeaderValue
        };
        if (actorHeader is not null)
        {
            headers[OnBehalfOfHeaders.ActorHeader] = actorHeader;
        }

        if (assertion is not null)
        {
            headers[OnBehalfOfHeaders.AssertionHeader] = assertion;
        }

        if (tenant is not null)
        {
            headers[Tenancy.Constants.DefaultTenantIdHeader] = tenant;
        }

        var context = Substitute.For<IHttpRequestContext>();
        context.Items.Returns(new Dictionary<object, object?>());
        context.Headers.Returns(headers);
        context.Method.Returns(Method);
        context.Host.Returns(Host);
        context.Path.Returns(Path);
        return context;
    }

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
    protected static string ActorHeaderValue(string userId = ActorId) =>
        Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new
        {
            identityProvider = "support-idp",
            userId,
            userDetails = ActorDetails,
            userRoles = new[] { ActorRole },
            claims = new[] { new { typ = "name", val = "Support Person" } }
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
