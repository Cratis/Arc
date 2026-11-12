// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Evaluates ASP.NET Core policies and authentication schemes alongside native Arc policies.
/// </summary>
/// <param name="native">Native Arc policy resolution.</param>
/// <param name="anonymousPolicies">Explicit anonymous policy opt-ins.</param>
public class AspNetAuthorizationPolicyRuntime(
    ArcAuthorizationPolicyRuntime native,
    IEnumerable<AnonymousAspNetAuthorizationPolicyRegistration> anonymousPolicies) : IAuthorizationPolicyRuntime, IAuthorizationEmissionRuntime, IAnonymousAspNetAuthorizationPolicyValidator
{
    readonly string[] _anonymousPolicyNames = [.. anonymousPolicies.Select(registration => registration.Name)];

    /// <summary>
    /// Initializes the runtime with no ASP.NET Core anonymous policy opt-ins.
    /// </summary>
    /// <param name="native">The native Arc policy runtime.</param>
    public AspNetAuthorizationPolicyRuntime(ArcAuthorizationPolicyRuntime native) : this(native, [])
    {
    }

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
    async Task IAnonymousAspNetAuthorizationPolicyValidator.ValidateAnonymousPolicies(IServiceProvider services, CancellationToken cancellationToken)
    {
        var provider = services.GetService<IAuthorizationPolicyProvider>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in _anonymousPolicyNames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsInvalidOrAmbiguousAnonymousPolicyName(name, seen))
            {
                throw new InvalidAuthorizationConfiguration($"Anonymous ASP.NET Core authorization policy '{name}' is invalid or ambiguous.");
            }

            var policy = provider is null ? null : await provider.GetPolicyAsync(name);
            cancellationToken.ThrowIfCancellationRequested();
            if (policy?.Requirements.OfType<DenyAnonymousAuthorizationRequirement>().Any() != false)
            {
                throw new InvalidAuthorizationConfiguration($"Anonymous ASP.NET Core authorization policy '{name}' is unknown or requires authentication.");
            }

            if (policy.AuthenticationSchemes.Count > 0)
            {
                throw new InvalidAuthorizationConfiguration($"Anonymous ASP.NET Core authorization policy '{name}' selects authentication schemes.");
            }
        }
    }

    /// <inheritdoc/>
    public async Task Validate(IReadOnlyList<AuthorizationRequirement> requirements, IServiceProvider services, CancellationToken cancellationToken) =>
        _ = await Resolve(requirements, services, cancellationToken);

    /// <inheritdoc/>
    public async Task<IAuthorizationPolicyResolution> Resolve(IReadOnlyList<AuthorizationRequirement> requirements, IServiceProvider services, CancellationToken cancellationToken)
    {
        var provider = services.GetService<IAuthorizationPolicyProvider>();
        var collected = new CollectedRequirements();
        foreach (var requirement in requirements)
        {
            cancellationToken.ThrowIfCancellationRequested();
            collected.Schemes.AddRange(requirement.AuthenticationSchemes);
            if (string.IsNullOrWhiteSpace(requirement.Policy))
            {
                continue;
            }

            await CollectPolicy(collected, requirement, provider, cancellationToken);
        }

        var selectedSchemes = collected.Schemes.Distinct(StringComparer.Ordinal).ToArray();
        await EnsureSchemesAreKnown(selectedSchemes, services.GetService<IAuthenticationSchemeProvider>(), cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        var nativeResolution = await native.Resolve(collected.NativeRequirements, services, cancellationToken);
        var evaluatesAnonymous = EvaluatesAnonymous(requirements, selectedSchemes, collected, nativeResolution);
        return new AspNetResolution(nativeResolution, collected.AspPolicies.ToArray(), selectedSchemes, evaluatesAnonymous);
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

    static async Task EnsureSchemesAreKnown(string[] selectedSchemes, IAuthenticationSchemeProvider? schemeProvider, CancellationToken cancellationToken)
    {
        foreach (var scheme in selectedSchemes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (schemeProvider is null || await schemeProvider.GetSchemeAsync(scheme) is null)
            {
                throw new InvalidAuthorizationConfiguration($"Unknown authentication scheme '{scheme}'.");
            }
        }
    }

    static bool EvaluatesAnonymous(
        IReadOnlyList<AuthorizationRequirement> requirements,
        string[] selectedSchemes,
        CollectedRequirements collected,
        IAuthorizationPolicyResolution nativeResolution) =>
        requirements.Count > 0 && selectedSchemes.Length == 0 && collected.PoliciesEvaluateAnonymous &&
        requirements.All(requirement => requirement.AnyOfRoles.Count == 0 && !string.IsNullOrWhiteSpace(requirement.Policy)) &&
        (collected.NativeRequirements.Count == 0 || nativeResolution is IAnonymousPolicyResolution { EvaluatesAnonymous: true });

    bool IsInvalidOrAmbiguousAnonymousPolicyName(string name, HashSet<string> seen) =>
        string.IsNullOrWhiteSpace(name) || !seen.Add(name) || native.HasPolicyIgnoringCase(name);

    async Task CollectPolicy(CollectedRequirements collected, AuthorizationRequirement requirement, IAuthorizationPolicyProvider? provider, CancellationToken cancellationToken)
    {
        var policyName = requirement.Policy!;
        var nativePolicy = native.HasPolicy(policyName);
        var aspPolicy = provider is null ? null : await provider.GetPolicyAsync(policyName);
        cancellationToken.ThrowIfCancellationRequested();
        if (nativePolicy == (aspPolicy is not null))
        {
            throw new InvalidAuthorizationConfiguration($"Authorization policy '{policyName}' is unknown or ambiguous.");
        }

        if (nativePolicy)
        {
            collected.NativeRequirements.Add(requirement with { AuthenticationSchemes = [] });
            return;
        }

        collected.AddAspPolicy(aspPolicy!, _anonymousPolicyNames.Contains(policyName, StringComparer.Ordinal));
    }

    sealed class CollectedRequirements
    {
        public List<string> Schemes { get; } = [];

        public List<AuthorizationRequirement> NativeRequirements { get; } = [];

        public List<AuthorizationPolicy> AspPolicies { get; } = [];

        public bool PoliciesEvaluateAnonymous { get; private set; } = true;

        public void AddAspPolicy(AuthorizationPolicy policy, bool optedIntoAnonymous)
        {
            AspPolicies.Add(policy);
            PoliciesEvaluateAnonymous &= optedIntoAnonymous &&
                !policy.Requirements.OfType<DenyAnonymousAuthorizationRequirement>().Any();
            Schemes.AddRange(policy.AuthenticationSchemes);
        }
    }

    sealed class AspNetResolution(
        IAuthorizationPolicyResolution nativeResolution,
        AuthorizationPolicy[] aspPolicies,
        string[] selectedSchemes,
        bool evaluatesAnonymous) : IAuthorizationPolicyResolution, IAnonymousPolicyResolution, IResourceFreePolicyResolution
    {
        public bool CanEvaluateWithoutResource =>
            nativeResolution is IResourceFreePolicyResolution { CanEvaluateWithoutResource: true } &&
            aspPolicies.All(policy => policy.Requirements.All(requirement => requirement is
                DenyAnonymousAuthorizationRequirement or RolesAuthorizationRequirement or ClaimsAuthorizationRequirement or
                NameAuthorizationRequirement or IResourceFreeRequirement));

        public bool EvaluatesAnonymous => evaluatesAnonymous;

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
                identities.AddRange(await AuthenticateScheme(authentication, httpContext, scheme, cancellationToken));
            }

            return identities.Count > 0 ? new ClaimsPrincipal(identities) : null;
        }

        public async Task<bool> IsAuthorized(AuthorizationPolicyContext context, IServiceProvider services, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nativeCheckpoint = new AuthorizationPolicyIdentityCheckpoint(context, services);
            var nativeAllowed = await nativeResolution.IsAuthorized(context, services, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!nativeCheckpoint.IsUnchanged() || !nativeAllowed)
            {
                return false;
            }

            foreach (var policy in aspPolicies)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await IsAllowedByPolicy(policy, context, services, cancellationToken))
                {
                    return false;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return true;
        }

        static async Task<IEnumerable<ClaimsIdentity>> AuthenticateScheme(IAuthenticationService authentication, HttpContext httpContext, string scheme, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await authentication.AuthenticateAsync(httpContext, scheme);
            cancellationToken.ThrowIfCancellationRequested();
            return result.Succeeded && result.Principal is not null
                ? result.Principal.Identities.Where(identity => identity.IsAuthenticated)
                : [];
        }

        static async Task<bool> IsAllowedByPolicy(AuthorizationPolicy policy, AuthorizationPolicyContext context, IServiceProvider services, CancellationToken cancellationToken)
        {
            var checkpoint = new AuthorizationPolicyIdentityCheckpoint(context, services);
            var service = services.GetRequiredService<IAuthorizationService>();
            if (!checkpoint.IsUnchanged())
            {
                cancellationToken.ThrowIfCancellationRequested();
                return false;
            }

            var allowed = (await service.AuthorizeAsync(context.Principal, context.Resource, policy)).Succeeded;
            cancellationToken.ThrowIfCancellationRequested();
            return checkpoint.IsUnchanged() && allowed;
        }
    }
}
