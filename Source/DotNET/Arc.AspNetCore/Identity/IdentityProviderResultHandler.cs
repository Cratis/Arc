// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using Cratis.DependencyInjection;

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents an implementation of <see cref="IIdentityProviderResultHandler"/>.
/// </summary>
/// <param name="httpContextAccessor">The <see cref="IHttpContextAccessor"/>.</param>
/// <param name="identityProvider">The <see cref="IProvideIdentityDetails"/>.</param>
/// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
[Singleton]
public class IdentityProviderResultHandler(
    IHttpContextAccessor httpContextAccessor,
    IProvideIdentityDetails identityProvider,
    JsonSerializerOptions serializerOptions) : IIdentityProviderResultHandler
{
    /// <summary>
    /// The name of the identity cookie.
    /// </summary>
    public const string IdentityCookieName = ".cratis-identity";

    readonly JsonSerializerOptions _serializerOptions = new(serializerOptions)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <inheritdoc/>
    public async Task<IdentityProviderResult> GenerateFromCurrentContext()
    {
        var request = httpContextAccessor.HttpContext!.Request;

        if (!request.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            return IdentityProviderResult.Anonymous;
        }

        var claimsPrincipal = request.HttpContext.User;
        if (claimsPrincipal is not null)
        {
            var identityId = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "unknown";
            var identityName = claimsPrincipal.Identity?.Name ?? "unknown";
            var claims = claimsPrincipal.Claims.Select(claim => new KeyValuePair<string, string>(claim.Type, claim.Value));

            var context = new IdentityProviderContext(identityId, identityName, claims);
            var result = await identityProvider.Provide(context);

            if (result.IsUserAuthorized)
            {
                return new IdentityProviderResult(context.Id, context.Name, true, result.IsUserAuthorized, result.Details);
            }
        }

        return IdentityProviderResult.Unauthorized;
    }

    /// <inheritdoc/>
    public async Task Write(IdentityProviderResult result)
    {
        var request = httpContextAccessor.HttpContext!.Request;
        var response = httpContextAccessor.HttpContext!.Response;
        response.ContentType = "application/json; charset=utf-8";
        var json = JsonSerializer.Serialize(result, _serializerOptions);
        var base64Json = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
        response.Cookies.Append(IdentityCookieName, base64Json, new CookieOptions
        {
            HttpOnly = false,
            Secure = request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });

        await response.WriteAsync(json);
    }

    /// <inheritdoc/>
    public async Task ModifyDetails<TDetails>(Func<TDetails, TDetails> details)
    {
        var request = httpContextAccessor.HttpContext!.Request;
        request.Cookies.TryGetValue(IdentityCookieName, out var base64Json);
        if (base64Json is null)
        {
            return;
        }

        var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64Json));
        var identityResult = JsonSerializer.Deserialize<IdentityProviderResult<TDetails>>(json, _serializerOptions);
        if (identityResult is null)
        {
            return;
        }
        var modifiedIdentityResult = identityResult with { Details = details(identityResult.Details) };
        var actualIdentityResult = new IdentityProviderResult(
            modifiedIdentityResult.Id,
            modifiedIdentityResult.Name,
            modifiedIdentityResult.IsAuthenticated,
            modifiedIdentityResult.IsAuthorized,
            modifiedIdentityResult.Details!);
        await Write(actualIdentityResult);
    }
}