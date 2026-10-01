// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Cratis.Arc.Http;
using Cratis.DependencyInjection;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents an implementation of <see cref="IIdentityProvider"/>.
/// </summary>
/// <param name="httpRequestContextAccessor">The <see cref="IHttpRequestContextAccessor"/>.</param>
/// <param name="options">The <see cref="IOptions{ArcOptions}"/>.</param>
/// <param name="activitySource">The <see cref="IActivitySource{T}"/> for tracing.</param>
[Singleton]
public class IdentityProvider(
    IHttpRequestContextAccessor httpRequestContextAccessor,
    IOptions<ArcOptions> options,
    IActivitySource<IdentityProvider> activitySource) : IIdentityProvider
{
    /// <summary>
    /// The name of the identity cookie earlier versions of Arc wrote.
    /// </summary>
    /// <remarks>
    /// Arc neither reads nor writes this cookie: the identity is always derived from the authenticated request, and
    /// frontends get it from the <c>/.cratis/me</c> endpoint.
    /// </remarks>
    [Obsolete("Arc no longer reads or writes the identity cookie. Get the identity from the /.cratis/me endpoint instead.")]
    public const string IdentityCookieName = LegacyIdentityCookieName;

    const string LegacyIdentityCookieName = ".cratis-identity";

    readonly JsonSerializerOptions _serializerOptions = IdentityJsonSerializerOptions.CreateFrom(options.Value.JsonSerializerOptions);

    /// <inheritdoc/>
    public async Task<IdentityProviderResult> Get()
    {
        using var span = activitySource.Resolve();
        var context = httpRequestContextAccessor.Current;
        if (context is null)
        {
            return IdentityProviderResult.Anonymous;
        }

        return await CreateFromCurrentContext(context);
    }

    /// <inheritdoc/>
    public async Task<IdentityProviderResult<TDetails>> Get<TDetails>()
    {
        using var span = activitySource.Resolve();
        var context = httpRequestContextAccessor.Current;
        if (context is null)
        {
            return new IdentityProviderResult<TDetails>(IdentityId.Empty, IdentityName.Empty, false, false, [], default!);
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

        context.SetNoStoreResponseHeaders();
        context.ContentType = "application/json; charset=utf-8";

        // A readable identity cookie written by an earlier version would otherwise linger in the browser, and a
        // frontend that still reads it would keep showing it instead of asking for the identity again.
        if (context.Cookies.ContainsKey(LegacyIdentityCookieName))
        {
            context.RemoveCookie(LegacyIdentityCookieName);
        }

        var json = JsonSerializer.Serialize(result, TypeInfoFor<IdentityProviderResult>());
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

    async Task<IdentityProviderResult> CreateFromCurrentContext(IHttpRequestContext context)
    {
        if (!context.User?.Identity?.IsAuthenticated ?? true)
        {
            return IdentityProviderResult.Anonymous;
        }

        var claimsPrincipal = context.User;
        var identityId = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "unknown";
        var identityName = claimsPrincipal.Identity?.Name;
        if (string.IsNullOrEmpty(identityName))
        {
            context.Headers.TryGetValue(MicrosoftIdentityPlatformHeaders.IdentityNameHeader, out identityName);
        }
        identityName = string.IsNullOrEmpty(identityName) ? "unknown" : identityName;
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
            return jsonElement.Deserialize(TypeInfoFor<TDetails>())!;
        }

        var serializedDetails = JsonSerializer.Serialize(details, TypeInfoFor<object>());
        return JsonSerializer.Deserialize(serializedDetails, TypeInfoFor<TDetails>())!;
    }

    /// <summary>
    /// Gets the contract for a type from the identity serializer options: Arc's own identity types come from
    /// <see cref="IdentityJsonSerializerContext"/>, the application's identity details from its own resolver chain.
    /// </summary>
    /// <typeparam name="T">The type to get the contract for.</typeparam>
    /// <returns>The <see cref="JsonTypeInfo{T}"/>.</returns>
    JsonTypeInfo<T> TypeInfoFor<T>() => (JsonTypeInfo<T>)_serializerOptions.GetTypeInfo(typeof(T));
}
