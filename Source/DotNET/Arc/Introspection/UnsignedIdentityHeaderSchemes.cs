// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Introspection;

/// <summary>
/// Detects unsigned identity-header authentication reachable by protected catalog endpoints.
/// </summary>
internal static class UnsignedIdentityHeaderSchemes
{
    static readonly object _authenticatedBeforeRoutingKey = new();

    /// <summary>
    /// Checks the default authentication scheme and any schemes used by the applied default authorization policy.
    /// </summary>
    /// <param name="services">The host services.</param>
    /// <param name="schemes">The registered authentication schemes.</param>
    /// <param name="defaultScheme">The default authentication scheme.</param>
    /// <returns>Whether unsigned identity headers are reachable.</returns>
    /// <exception cref="InvalidIntrospectionConfiguration">A dynamic selector could forward to a registered unsigned header scheme.</exception>
    internal static bool IsReachable(IServiceProvider services, IAuthenticationSchemeProvider schemes, AuthenticationScheme defaultScheme)
    {
        var registeredSchemes = schemes.GetAllSchemesAsync().GetAwaiter().GetResult();
        var headerRegistered = registeredSchemes.Any(scheme => typeof(MicrosoftIDentityPlatformAuthHandler).IsAssignableFrom(scheme.HandlerType));
        if (FollowsHeaderScheme(services, schemes, defaultScheme, headerRegistered))
        {
            return true;
        }

        var provider = services.GetRequiredService<IAuthorizationPolicyProvider>();
        var policy = provider.GetDefaultPolicyAsync().GetAwaiter().GetResult();
        return policy.AuthenticationSchemes.Any(name =>
        {
            var scheme = schemes.GetSchemeAsync(name).GetAwaiter().GetResult();
            return scheme is not null && FollowsHeaderScheme(services, schemes, scheme, headerRegistered);
        });
    }

    /// <summary>
    /// Records successful header authentication before endpoint routing has completed.
    /// </summary>
    /// <param name="context">The request whose endpoint is not yet known.</param>
    internal static void RecordAuthenticationBeforeRouting(HttpContext context) => context.Items[_authenticatedBeforeRoutingKey] = true;

    /// <summary>
    /// Checks request-local authentication provenance, independently of claims transformation.
    /// </summary>
    /// <param name="context">The catalog request.</param>
    /// <returns>Whether unsigned headers authenticated before routing selected an endpoint.</returns>
    internal static bool AuthenticatedBeforeRouting(HttpContext context) => context.Items.ContainsKey(_authenticatedBeforeRoutingKey);

    static bool FollowsHeaderScheme(IServiceProvider services, IAuthenticationSchemeProvider schemes, AuthenticationScheme scheme, bool headerRegistered)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (visited.Add(scheme.Name))
        {
            if (typeof(MicrosoftIDentityPlatformAuthHandler).IsAssignableFrom(scheme.HandlerType))
            {
                return true;
            }

            var options = GetOptions(services, scheme);
            if (options is null)
            {
                return false;
            }

            // AuthenticationHandler<TOptions>.ResolveTarget prefers ForwardAuthenticate, then
            // ForwardDefaultSelector, then ForwardDefault. A selector needs a request context,
            // so its target cannot be evaluated safely during startup.
            if (options.ForwardAuthenticate is null && options.ForwardDefaultSelector is not null && headerRegistered)
            {
                throw new InvalidIntrospectionConfiguration($"Authentication scheme '{scheme.Name}' uses ForwardDefaultSelector; its target cannot be verified at startup while an unsigned identity-header scheme is registered. Set Cratis:Arc:Introspection:TrustForwardedIdentityHeaders=true to opt in, or use a static scheme or ForwardAuthenticate.");
            }

            var forwarded = options.ForwardAuthenticate ?? options.ForwardDefault;
            if (forwarded is null || forwarded == scheme.Name)
            {
                return false;
            }

            var next = schemes.GetSchemeAsync(forwarded).GetAwaiter().GetResult();
            if (next is null)
            {
                return false;
            }
            scheme = next;
        }

        return false;
    }

    static AuthenticationSchemeOptions? GetOptions(IServiceProvider services, AuthenticationScheme scheme)
    {
        var handlerType = scheme.HandlerType;
        while (handlerType is not null && (!handlerType.IsGenericType || handlerType.GetGenericTypeDefinition() != typeof(AuthenticationHandler<>)))
        {
            handlerType = handlerType.BaseType;
        }

        if (handlerType is null)
        {
            return null;
        }

        var monitorType = typeof(IOptionsMonitor<>).MakeGenericType(handlerType.GenericTypeArguments[0]);
        var monitor = services.GetRequiredService(monitorType);
        return (AuthenticationSchemeOptions)monitorType.GetMethod(nameof(IOptionsMonitor<AuthenticationSchemeOptions>.Get))!.Invoke(monitor, [scheme.Name])!;
    }
}
