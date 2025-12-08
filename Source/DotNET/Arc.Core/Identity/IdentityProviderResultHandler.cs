// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.DependencyInjection;

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents an implementation of <see cref="IIdentityProviderResultHandler"/>.
/// </summary>
/// <param name="httpRequestContextAccessor">The <see cref="IHttpRequestContextAccessor"/>.</param>
/// <param name="identityProvider">The <see cref="IProvideIdentityDetails"/>.</param>
[Singleton]
public class IdentityProviderResultHandler(
    IHttpRequestContextAccessor httpRequestContextAccessor,
    IProvideIdentityDetails identityProvider) : IIdentityProviderResultHandler
{
    /// <summary>
    /// The name of the identity cookie.
    /// </summary>
    public const string IdentityCookieName = ".cratis-identity";

    readonly JsonSerializerOptions _serializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc/>
    public async Task<IdentityProviderResult> GenerateFromCurrentContext()
    {
        var context = httpRequestContextAccessor.Current;
        if (context is null)
        {
            return IdentityProviderResult.Anonymous;
        }

        if (!context.User?.Identity?.IsAuthenticated ?? true)
        {
            return IdentityProviderResult.Anonymous;
        }

        var claimsPrincipal = context.User;
        if (claimsPrincipal is not null)
        {
            var identityId = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "unknown";
            var identityName = claimsPrincipal.Identity?.Name ?? "unknown";
            var claims = claimsPrincipal.Claims.Select(claim => new KeyValuePair<string, string>(claim.Type, claim.Value));

            var providerContext = new IdentityProviderContext(identityId, identityName, claims);
            var result = await identityProvider.Provide(providerContext);

            if (result.IsUserAuthorized)
            {
                return new IdentityProviderResult(providerContext.Id, providerContext.Name, true, result.IsUserAuthorized, result.Details);
            }
        }

        return IdentityProviderResult.Unauthorized;
    }

    /// <inheritdoc/>
    public async Task Write(IdentityProviderResult result)
    {
        var context = httpRequestContextAccessor.Current;
        if (context is null)
        {
            return;
        }

        context.ContentType = "application/json; charset=utf-8";
        var json = JsonSerializer.Serialize(result, _serializerOptions);
        var base64Json = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));

        context.AppendCookie(IdentityCookieName, base64Json, new CookieOptions
        {
            HttpOnly = false,
            Secure = context.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });

        await context.WriteAsync(json);
    }

    /// <inheritdoc/>
    public async Task ModifyDetails<TDetails>(Func<TDetails, TDetails> details)
    {
        var context = httpRequestContextAccessor.Current;
        if (context is null)
        {
            return;
        }

        var result = await GenerateFromCurrentContext();
        if (result.Details is TDetails typedDetails)
        {
            var modifiedDetails = details(typedDetails);
            var modifiedResult = new IdentityProviderResult(
                result.Id,
                result.Name,
                result.IsAuthenticated,
                result.IsAuthorized,
                modifiedDetails);

            await Write(modifiedResult);
        }
    }
}
