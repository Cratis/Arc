// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Evaluates ASP.NET Core policies and authentication schemes alongside native Arc policies.
/// </summary>
/// <param name="native">Native Arc policy resolution.</param>
public class AspNetAuthorizationPolicyRuntime(ArcAuthorizationPolicyRuntime native) : IAuthorizationPolicyRuntime, IAuthorizationEmissionRuntime
{
    /// <inheritdoc/>
    public IDisposable? BeginPrincipalScope(ClaimsPrincipal principal, IServiceProvider services)
    {
        var accessor = services.GetService<IHttpContextAccessor>();
        if (accessor?.HttpContext is null)
        {
            return null;
        }

        if (accessor is not OperationHttpContextAccessor isolated)
        {
            throw new InvalidAuthorizationConfiguration("Scheme-selected HTTP execution requires Arc's operation-local HTTP context accessor.");
        }

        return isolated.Begin(principal, services);
    }

    /// <inheritdoc/>
    public async Task Validate(IReadOnlyList<AuthorizationRequirement> requirements, IServiceProvider services, CancellationToken cancellationToken) =>
        _ = await Resolve(requirements, services, cancellationToken);

    /// <inheritdoc/>
    public async Task<IAuthorizationPolicyResolution> Resolve(IReadOnlyList<AuthorizationRequirement> requirements, IServiceProvider services, CancellationToken cancellationToken)
    {
        var provider = services.GetService<IAuthorizationPolicyProvider>();
        var schemes = new List<string>();
        var nativeRequirements = new List<AuthorizationRequirement>();
        var aspPolicies = new List<AuthorizationPolicy>();
        foreach (var requirement in requirements)
        {
            cancellationToken.ThrowIfCancellationRequested();
            schemes.AddRange(requirement.AuthenticationSchemes);
            if (string.IsNullOrWhiteSpace(requirement.Policy))
            {
                continue;
            }

            var nativePolicy = native.HasPolicy(requirement.Policy);
            var aspPolicy = provider is null ? null : await provider.GetPolicyAsync(requirement.Policy);
            cancellationToken.ThrowIfCancellationRequested();
            if (nativePolicy == (aspPolicy is not null))
            {
                throw new InvalidAuthorizationConfiguration($"Authorization policy '{requirement.Policy}' is unknown or ambiguous.");
            }

            if (nativePolicy)
            {
                nativeRequirements.Add(requirement with { AuthenticationSchemes = [] });
            }
            else
            {
                aspPolicies.Add(aspPolicy!);
                schemes.AddRange(aspPolicy!.AuthenticationSchemes);
            }
        }

        var selectedSchemes = schemes.Distinct(StringComparer.Ordinal).ToArray();
        var schemeProvider = services.GetService<IAuthenticationSchemeProvider>();
        foreach (var scheme in selectedSchemes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (schemeProvider is null || await schemeProvider.GetSchemeAsync(scheme) is null)
            {
                throw new InvalidAuthorizationConfiguration($"Unknown authentication scheme '{scheme}'.");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        var nativeResolution = await native.Resolve(nativeRequirements, services, cancellationToken);
        return new AspNetResolution(nativeResolution, aspPolicies.ToArray(), selectedSchemes);
    }

    /// <inheritdoc/>
    public IDisposable? BeginEmissionScope(ClaimsPrincipal principal, IServiceProvider services, bool hasLiveRequest)
    {
        var accessor = services.GetService<IHttpContextAccessor>();
        if (accessor is not OperationHttpContextAccessor isolated)
        {
            if (accessor?.HttpContext is not null)
            {
                throw new InvalidAuthorizationConfiguration("Observable HTTP emission requires Arc's operation-local HTTP context accessor.");
            }

            return null;
        }

        return hasLiveRequest ? isolated.Begin(principal, services) : isolated.Suppress();
    }

    /// <inheritdoc/>
    public Func<ClaimsPrincipal, IServiceProvider, IDisposable?>? CaptureLiveRequest(IServiceProvider services)
    {
        var accessor = services.GetService<IHttpContextAccessor>();
        var context = accessor?.HttpContext;
        if (context is null)
        {
            return null;
        }

        if (accessor is not OperationHttpContextAccessor isolated)
        {
            return null;
        }

        return (principal, provider) => isolated.Begin(principal, provider, context);
    }

    sealed class AspNetResolution(
        IAuthorizationPolicyResolution nativeResolution,
        AuthorizationPolicy[] aspPolicies,
        string[] selectedSchemes) : IAuthorizationPolicyResolution
    {
        public async Task<ClaimsPrincipal?> SelectPrincipal(ClaimsPrincipal? principal, IServiceProvider services, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (selectedSchemes.Length == 0)
            {
                return principal;
            }

            var httpContext = services.GetService<IHttpContextAccessor>()?.HttpContext;
            var authentication = services.GetService<IAuthenticationService>();
            if (httpContext is null || authentication is null)
            {
                return null;
            }

            var identities = new List<ClaimsIdentity>();
            foreach (var scheme in selectedSchemes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await authentication.AuthenticateAsync(httpContext, scheme);
                cancellationToken.ThrowIfCancellationRequested();
                if (result.Succeeded && result.Principal is not null)
                {
                    identities.AddRange(result.Principal.Identities.Where(identity => identity.IsAuthenticated));
                }
            }

            return identities.Count > 0 ? new ClaimsPrincipal(identities) : null;
        }

        public async Task<bool> IsAuthorized(AuthorizationPolicyContext context, IServiceProvider services, CancellationToken cancellationToken)
        {
            if (!await nativeResolution.IsAuthorized(context, services, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return false;
            }

            var service = aspPolicies.Length > 0 ? services.GetRequiredService<IAuthorizationService>() : null;
            foreach (var policy in aspPolicies)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!(await service!.AuthorizeAsync(context.Principal, context.Resource, policy)).Succeeded)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return false;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return true;
        }
    }
}
