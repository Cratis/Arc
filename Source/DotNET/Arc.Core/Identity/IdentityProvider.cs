// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents an implementation of <see cref="IIdentityProvider"/>.
/// </summary>
/// <param name="httpRequestContextAccessor">The <see cref="IHttpRequestContextAccessor"/>.</param>
/// <param name="options">The <see cref="IOptions{ArcOptions}"/>.</param>
[Singleton]
public class IdentityProvider(
    IHttpRequestContextAccessor httpRequestContextAccessor,
    IOptions<ArcOptions> options) : IIdentityProvider
{
    /// <summary>
    /// The name of the identity cookie.
    /// </summary>
    public const string IdentityCookieName = ".cratis-identity";

    readonly JsonSerializerOptions _serializerOptions = new(options.Value.JsonSerializerOptions)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <inheritdoc/>
    public async Task<IdentityProviderResult> Get()
    {
        var context = httpRequestContextAccessor.Current;
        if (context is null)
        {
            return IdentityProviderResult.Anonymous;
        }

        if (TryGetFromCookie(context, out var cookieResult))
        {
            return cookieResult;
        }

        return await CreateFromCurrentContext(context);
    }

    /// <inheritdoc/>
    public async Task<IdentityProviderResult<TDetails>> Get<TDetails>()
    {
        var context = httpRequestContextAccessor.Current;
        if (context is null)
        {
            return new IdentityProviderResult<TDetails>(IdentityId.Empty, IdentityName.Empty, false, false, [], default!);
        }

        if (TryGetFromCookie(context, out IdentityProviderResult<TDetails> typedCookieResult))
        {
            return typedCookieResult;
        }

        var result = await CreateFromCurrentContext(context);
        var details = ConvertDetails<TDetails>(result.Details);

        return new IdentityProviderResult<TDetails>(
            result.Id,
            result.Name,
            result.IsAuthenticated,
            result.IsAuthorized,
            result.Roles,
            details);
    }

    /// <inheritdoc/>
    public async Task SetCookieForHttpResponse(IdentityProviderResult result)
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

        await context.Write(json);
    }

    /// <inheritdoc/>
    public async Task ModifyDetails<TDetails>(Func<TDetails, TDetails> details)
    {
        var context = httpRequestContextAccessor.Current;
        if (context is null)
        {
            return;
        }

        var result = await Get();
        if (result.Details is TDetails typedDetails)
        {
            var modifiedDetails = details(typedDetails);
            var modifiedResult = new IdentityProviderResult(
                result.Id,
                result.Name,
                result.IsAuthenticated,
                result.IsAuthorized,
                result.Roles,
                modifiedDetails);

            await SetCookieForHttpResponse(modifiedResult);
        }
    }

    bool TryGetFromCookie(IHttpRequestContext context, out IdentityProviderResult result)
    {
        result = IdentityProviderResult.Anonymous;

        if (!context.Cookies.TryGetValue(IdentityCookieName, out var encodedCookie) || string.IsNullOrWhiteSpace(encodedCookie))
        {
            return false;
        }

        try
        {
            var decodedJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedCookie));
            result = JsonSerializer.Deserialize<IdentityProviderResult>(decodedJson, _serializerOptions) ?? IdentityProviderResult.Anonymous;
        }
        catch
        {
            result = IdentityProviderResult.Anonymous;
        }

        return true;
    }

    bool TryGetFromCookie<TDetails>(IHttpRequestContext context, out IdentityProviderResult<TDetails> result)
    {
        result = new IdentityProviderResult<TDetails>(IdentityId.Empty, IdentityName.Empty, false, false, [], default!);

        if (!context.Cookies.TryGetValue(IdentityCookieName, out var encodedCookie) || string.IsNullOrWhiteSpace(encodedCookie))
        {
            return false;
        }

        try
        {
            var decodedJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedCookie));
            result = JsonSerializer.Deserialize<IdentityProviderResult<TDetails>>(decodedJson, _serializerOptions)
                ?? new IdentityProviderResult<TDetails>(IdentityId.Empty, IdentityName.Empty, false, false, [], default!);
        }
        catch
        {
            result = new IdentityProviderResult<TDetails>(IdentityId.Empty, IdentityName.Empty, false, false, [], default!);
        }

        return true;
    }

    async Task<IdentityProviderResult> CreateFromCurrentContext(IHttpRequestContext context)
    {
        if (!context.User?.Identity?.IsAuthenticated ?? true)
        {
            return IdentityProviderResult.Anonymous;
        }

        var claimsPrincipal = context.User;
        var identityId = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "unknown";
        var identityName = claimsPrincipal.Identity?.Name ?? "unknown";
        var claims = claimsPrincipal.Claims.Select(claim => new KeyValuePair<string, string>(claim.Type, claim.Value));
        var roles = claimsPrincipal.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

        var providerContext = new IdentityProviderContext(identityId, identityName, claims);
        var identityProvider = context.RequestServices.GetRequiredService<IProvideIdentityDetails>();
        var details = await identityProvider.Provide(providerContext);

        if (details.IsUserAuthorized)
        {
            return new IdentityProviderResult(providerContext.Id, providerContext.Name, true, true, roles, details.Details);
        }

        return IdentityProviderResult.Unauthorized;
    }

    TDetails ConvertDetails<TDetails>(object details)
    {
        if (details is TDetails typedDetails)
        {
            return typedDetails;
        }

        if (details is JsonElement jsonElement)
        {
            return jsonElement.Deserialize<TDetails>(_serializerOptions)!;
        }

        var serializedDetails = JsonSerializer.Serialize(details, _serializerOptions);
        return JsonSerializer.Deserialize<TDetails>(serializedDetails, _serializerOptions)!;
    }
}
