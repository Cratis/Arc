// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Http;

namespace Cratis.Arc.Identity;

/// <summary>
/// Serializes complete signed authentication per request, caching only a fully verified independent principal.
/// </summary>
internal static class ForwardedIdentityAuthentication
{
    static readonly object _cacheKey = new();

    /// <summary>
    /// Authenticates both forwarding protocols or reuses the complete success for exactly unchanged inputs.
    /// </summary>
    /// <param name="context">The request, including its underlying Items store.</param>
    /// <param name="options">The current authentication configuration.</param>
    /// <param name="authenticationType">The host authentication type.</param>
    /// <param name="innerPath">The existing on-behalf path binding.</param>
    /// <param name="time">An optional verification clock.</param>
    /// <param name="replays">An optional outer replay cache.</param>
    /// <returns>A fresh principal copy, fixed failure code, or absent credential result.</returns>
    internal static ForwardedIdentityAuthenticationResult Authenticate(IHttpRequestContext context, ArcOptions options, string authenticationType, string innerPath, TimeProvider? time = null, ForwardedIdentityReplayCache? replays = null)
    {
        var items = context.Items;
        Cache cache;
        lock (items)
        {
            if (!items.TryGetValue(_cacheKey, out var entry) || entry is not Cache existing)
            {
                cache = new();
                items[_cacheKey] = cache;
            }
            else
            {
                cache = existing;
            }
        }
        lock (cache)
        {
            return AuthenticateLocked(context, options, authenticationType, innerPath, time ?? TimeProvider.System, replays ?? ForwardedIdentityReplayCache.Shared, cache);
        }
    }

    static ForwardedIdentityAuthenticationResult AuthenticateLocked(IHttpRequestContext context, ArcOptions options, string authenticationType, string innerPath, TimeProvider time, ForwardedIdentityReplayCache replays, Cache cache)
    {
        if (context is not ICanReadForwardedIdentityRequest)
        {
            cache.Principal = null;
            return ForwardedIdentitySnapshot.HasCredential(context.Headers.Keys) ? new(Failure: "forwarded-identity-invalid-input") : new();
        }
        var observed = ForwardedIdentitySnapshot.Capture(context, options);
        if (observed is not null && !ForwardedIdentitySnapshot.HasCredential(observed.Headers.Keys))
        {
            cache.Principal = null;
            return new();
        }
        if (observed is null || ForwardedIdentityConfiguration.Read(options) is not { } configuration ||
            !observed.Headers.TryGetValue(ForwardedIdentityEnvelope.Header, out var signature) ||
            ForwardedIdentityEnvelope.Parse(signature) is not { } envelope ||
            ForwardedIdentityCanonicalization.Create(observed, options, envelope) is not { } request)
        {
            cache.Principal = null;
            return new(Failure: "forwarded-identity-invalid-input");
        }
        var now = time.GetUtcNow().ToUnixTimeSeconds();
        var snapshot = ForwardedIdentityCanonicalization.Frame(configuration.Snapshot) + ForwardedIdentityCanonicalization.Frame(request.Snapshot) +
            ForwardedIdentityCanonicalization.Frame(authenticationType) + ForwardedIdentityCanonicalization.Frame(innerPath);
        if (cache.Principal is not null && cache.Snapshot == snapshot && ReferenceEquals(cache.JsonOptions, options.JsonSerializerOptions) && now <= cache.Expires && envelope.Timestamp <= now + 30)
        {
            return new(Clone(cache.Principal));
        }
        cache.Principal = null;
        var represented = ForwardedIdentityVerification.Verify(request, envelope, configuration, options, now, replays);
        if (represented is null)
        {
            return new(Failure: "forwarded-identity-rejected");
        }
        var identity = new ClaimsIdentity(ForwardedIdentityClaims.From(represented, request.Headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader]), authenticationType);
        if (ForwardedActor.IsPresent(request.Headers.ContainsKey))
        {
            var forward = new OnBehalfOfForward(
                request.Headers[MicrosoftIdentityPlatformHeaders.PrincipalHeader],
                request.Headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader],
                Header(request, OnBehalfOfHeaders.ActorHeader),
                Header(request, OnBehalfOfHeaders.AssertionHeader),
                options.Tenancy.HttpHeader is { Length: > 0 } tenantHeader ? Header(request, tenantHeader) : null,
                request.Method,
                innerPath);
            if (forward.Actor is null || ForwardedIdentityVerification.ReadPrincipal(forward.Actor, options.JsonSerializerOptions) is null ||
                ForwardedActor.Attach(forward, represented, identity, authenticationType, options, time, OnBehalfOfReplayCache.Shared) is not null)
            {
                return new(Failure: "forwarded-identity-invalid-actor");
            }
        }
        cache.Principal = new(identity);
        cache.Snapshot = snapshot;
        cache.JsonOptions = options.JsonSerializerOptions;
        cache.Expires = envelope.Timestamp + 60;
        return new(Clone(cache.Principal));
    }

    static string? Header(ForwardedIdentityRequest request, string name) => request.Headers.TryGetValue(name, out var value) ? value : null;

    static ClaimsPrincipal Clone(ClaimsPrincipal principal) => new(principal.Identities.Select(identity => identity.Clone()));

    sealed class Cache
    {
        internal ClaimsPrincipal? Principal { get; set; }
        internal string? Snapshot { get; set; }
        internal JsonSerializerOptions? JsonOptions { get; set; }
        internal long Expires { get; set; }
    }
}
