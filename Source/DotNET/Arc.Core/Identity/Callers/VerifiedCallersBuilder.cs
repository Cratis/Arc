// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Identity.ForwardedIdentityV2;
using Cratis.Arc.Identity.Workload;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// Configures one receiving resource and its signed browser-hop trust boundary.
/// </summary>
public sealed class VerifiedCallersBuilder
{
    string? _resource;
    string? _audience;
    string? _environment;
    ServiceDescriptor? _directory;
    SignedForwardedIdentityV2Builder? _forwarding;
    bool _allowPersonalAccessTokenCallers;
    WorkloadCallersBuilder? _workloads;
    ServiceDescriptor? _oauthVerifier;

    bool ValidTarget => VerifiedCallerInstallation.IsValid(_resource, _audience, _environment);
    bool ValidBrowserInstallation => ValidTarget && _directory is not null && _forwarding is not null;
    bool ValidPatInstallation => ValidTarget && _directory is not null && _forwarding is null;
    bool ValidOAuthInstallation => _workloads is null && _allowPersonalAccessTokenCallers && ValidBrowserInstallation;
    bool HasWorkloadConflict => _forwarding is not null || _allowPersonalAccessTokenCallers || _oauthVerifier is not null;

    /// <summary>
    /// Binds the exact resource, audience and environment without normalization.
    /// </summary>
    /// <param name="resource">The receiving resource.</param>
    /// <param name="audience">Its unique audience.</param>
    /// <param name="environment">The receiving environment.</param>
    /// <returns>This builder.</returns>
    public VerifiedCallersBuilder ForResource(string resource, string audience, string environment)
    {
        _resource = resource;
        _audience = audience;
        _environment = environment;
        return this;
    }

    /// <summary>
    /// Selects the persisted-person directory, resolved in the request scope.
    /// </summary>
    /// <typeparam name="TDirectory">The product-owned directory implementation.</typeparam>
    /// <returns>This builder.</returns>
    public VerifiedCallersBuilder UsePersonDirectory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDirectory>()
        where TDirectory : class, ICallerPersonDirectory
    {
        _directory = ServiceDescriptor.Scoped<ICallerPersonDirectory, TDirectory>();
        return this;
    }

    /// <summary>
    /// Configures the v2-only signed browser-hop verifier.
    /// </summary>
    /// <param name="configure">The explicit trust configuration.</param>
    /// <returns>This builder.</returns>
    public VerifiedCallersBuilder UseSignedForwardedIdentityV2(Action<SignedForwardedIdentityV2Builder> configure)
    {
        var forwarding = new SignedForwardedIdentityV2Builder();
        configure(forwarding);
        _forwarding = forwarding;
        return this;
    }

    /// <summary>
    /// Allows raw PAT callers alongside signed browser issuance in one installation.
    /// The PAT module supplies verification; capability endpoints accept only PAT presentations in this mode.
    /// </summary>
    /// <returns>This builder.</returns>
    public VerifiedCallersBuilder AllowPersonalAccessTokenCallers()
    {
        _allowPersonalAccessTokenCallers = true;
        return this;
    }

    /// <summary>Allows independently verified OAuth bearers covered by signed v2 forwarding alongside PAT callers.</summary>
    /// <typeparam name="TVerifier">The application's request-scoped token verifier.</typeparam>
    /// <returns>This builder.</returns>
    /// <remarks>Requires signed forwarding and <see cref="AllowPersonalAccessTokenCallers"/>. Raw OAuth, browser, delegated and sender-constrained callers are not admitted.</remarks>
    public VerifiedCallersBuilder AllowOAuthBearerCallers<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TVerifier>()
        where TVerifier : class, IOAuthBearerVerifier
    {
        _oauthVerifier = ServiceDescriptor.Scoped<IOAuthBearerVerifier, TVerifier>();
        return this;
    }

    /// <summary>Installs tenantless Kubernetes service-account callers on the capability lane.</summary>
    /// <param name="configure">The explicit Kubernetes trust and allowlist configuration.</param>
    /// <returns>This builder.</returns>
    public VerifiedCallersBuilder UseWorkloadCallers(Action<WorkloadCallersBuilder> configure)
    {
        var workloads = new WorkloadCallersBuilder();
        configure(workloads);
        _workloads = workloads;
        return this;
    }

    /// <summary>Validates and atomically registers the installation configuration.</summary>
    /// <param name="services">The receiving service collection.</param>
    /// <exception cref="InvalidVerifiedCallerInstallation">The configuration is incomplete or already installed.</exception>
    internal void Install(IServiceCollection services)
    {
        ValidateOAuthComposition(false);
        InstallComposition(services);
    }

    /// <summary>Installs PAT callers, optionally composing signed browser issuance through the explicit opt-in.</summary>
    /// <param name="services">The receiving service collection.</param>
    /// <param name="legacyPrefixes">Explicitly retired credential namespaces.</param>
    /// <returns>The exact receiving target.</returns>
    /// <exception cref="InvalidVerifiedCallerInstallation">The configuration is incomplete, duplicated or contains forwarding without the composition opt-in.</exception>
    internal CallerCredentialTarget InstallPersonalAccessTokens(IServiceCollection services, IEnumerable<string> legacyPrefixes)
    {
        ValidateOAuthComposition(true);
        if (_workloads is not null) throw new InvalidVerifiedCallerInstallation("Workload callers cannot be installed through the PAT installation.");
        if (_allowPersonalAccessTokenCallers)
        {
            InstallComposition(services);
        }
        else
        {
            if (!ValidPatInstallation)
            {
                throw new InvalidVerifiedCallerInstallation("PAT callers require a resource, audience, environment and person directory, without signed forwarding configuration.");
            }
            RegisterInstallation(services);
            services.AddSingleton(new PatCapableCallerInstallation());
        }
        services.AddSingleton(new ReservedCredentialPrefixes(legacyPrefixes));
        return new(_resource!, _audience!, _environment!);
    }

    void InstallComposition(IServiceCollection services)
    {
        if (_workloads is not null)
        {
            InstallWorkloads(services);
            return;
        }
        if (!ValidBrowserInstallation)
        {
            throw new InvalidVerifiedCallerInstallation("Verified callers require a resource, audience, environment, person directory and signed forwarding configuration.");
        }
        var settings = _forwarding!.Settings(_resource!, _audience!);
        RegisterInstallation(services);
        _forwarding.InstallNonceStore(services);
        services.AddForwardedIdentityV2(settings);
        if (_allowPersonalAccessTokenCallers) services.AddSingleton(new PatCapableCallerInstallation());
        if (_oauthVerifier is not null)
        {
            services.Add(_oauthVerifier);
            services.AddSingleton(new OAuthCapableCallerInstallation());
        }
    }

    void ValidateOAuthComposition(bool patModule)
    {
        if (_oauthVerifier is null) return;
        if (!patModule || !ValidOAuthInstallation)
        {
            throw new InvalidVerifiedCallerInstallation("OAuth bearer callers require signed forwarding and the PAT composition opt-in without workload callers.");
        }
    }

    void InstallWorkloads(IServiceCollection services)
    {
        if (!ValidTarget || HasWorkloadConflict)
        {
            throw new InvalidVerifiedCallerInstallation("Workload callers require an exact resource, audience and environment without browser or PAT composition.");
        }
        _workloads!.Validate();
        RegisterInstallation(services);
        _workloads.Install(services, new(_resource!, _audience!, _environment!));
    }

    void RegisterInstallation(IServiceCollection services)
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(InstallationMarker)))
        {
            throw new InvalidVerifiedCallerInstallation("Verified callers already have an installation.");
        }
        services.AddSingleton(new InstallationMarker());
        services.RemoveAll<VerifiedCallerInstallation>();
        services.RemoveAll<CallerResourceCatalog>();
        services.AddSingleton(new VerifiedCallerInstallation(_resource, _environment));
        services.AddSingleton(new CallerResourceCatalog(new Dictionary<string, string>(StringComparer.Ordinal) { [_resource!] = _audience! }));
        if (_directory is not null) services.Add(_directory);
        services.AddVerifiedCallers();
    }

    [IgnoreConvention]
    sealed class InstallationMarker;
}
