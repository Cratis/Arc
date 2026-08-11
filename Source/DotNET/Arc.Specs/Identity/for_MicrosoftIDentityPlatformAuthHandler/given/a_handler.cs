// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_MicrosoftIDentityPlatformAuthHandler.given;

public class a_handler : Specification
{
    protected const string IdentityIdFromHeader = "identity-id-from-header";
    protected const string IdentityNameFromHeader = "identity-name-from-header";
    protected const string UserDetails = "user@example.com";
    protected const string BenignClaimType = "email";
    protected const string BenignClaimValue = "user@example.com";
    protected const string IngressProviderKeyClaimType = "urn:cratis:identity:provider-key";
    protected const string IngressProviderKeyClaimValue = "workforce";

    protected IServiceProvider _services;

    void Establish() =>
        _services = new ServiceCollection()
            .AddSingleton(Options.Create(new ArcOptions()))
            .BuildServiceProvider();

    /// <summary>
    /// Runs one authentication through a freshly initialized handler, with the principal header holding the exact
    /// base64 encoded JSON wire shape the ingress forwards.
    /// </summary>
    /// <param name="identityProvider">The value of the <c>identityProvider</c> field, or null to omit the field entirely.</param>
    /// <param name="claims">The claims to put in the serialized principal.</param>
    /// <returns>The <see cref="AuthenticateResult"/> from the handler.</returns>
    protected async Task<AuthenticateResult> Authenticate(string? identityProvider, params (string Type, string Value)[] claims)
    {
        var httpContext = new DefaultHttpContext { RequestServices = _services };
        httpContext.Request.Headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader] = IdentityIdFromHeader;
        httpContext.Request.Headers[MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = IdentityNameFromHeader;
        httpContext.Request.Headers[MicrosoftIdentityPlatformHeaders.PrincipalHeader] = PrincipalHeaderValue(identityProvider, claims);

        var schemeOptions = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
        schemeOptions.Get(Arg.Any<string>()).Returns(new AuthenticationSchemeOptions());

        var handler = new MicrosoftIDentityPlatformAuthHandler(schemeOptions, NullLoggerFactory.Instance, UrlEncoder.Default);
        await handler.InitializeAsync(
            new AuthenticationScheme(
                MicrosoftIDentityPlatformAuthHandler.SchemeName,
                null,
                typeof(MicrosoftIDentityPlatformAuthHandler)),
            httpContext);

        return await handler.AuthenticateAsync();
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

    protected static string[] IdentityProviderClaimsFrom(AuthenticateResult result) =>
        [.. result.Principal!.FindAll(MicrosoftIdentityPlatformClaims.IdentityProvider).Select(claim => claim.Value)];

    protected static string[] ClaimValuesFrom(AuthenticateResult result, string claimType) =>
        [.. result.Principal!.FindAll(claimType).Select(claim => claim.Value)];
}
