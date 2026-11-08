// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Identity.ForwardedIdentityV2;
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

    /// <summary>Validates and atomically registers the installation configuration.</summary>
    /// <param name="services">The receiving service collection.</param>
    /// <exception cref="InvalidVerifiedCallerInstallation">The configuration is incomplete or already installed.</exception>
    internal void Install(IServiceCollection services)
    {
        if (!VerifiedCallerInstallation.IsValid(_resource, _audience, _environment) || _directory is null || _forwarding is null)
        {
            throw new InvalidVerifiedCallerInstallation("Verified callers require a resource, audience, environment, person directory and signed forwarding configuration.");
        }
        var settings = _forwarding.Settings(_resource!, _audience!);
        RegisterInstallation(services);
        _forwarding.InstallNonceStore(services);
        services.AddForwardedIdentityV2(settings);
        if (_allowPersonalAccessTokenCallers) services.AddSingleton(new PatCapableCallerInstallation());
    }

    /// <summary>Installs PAT callers, optionally composing signed browser issuance through the explicit opt-in.</summary>
    /// <param name="services">The receiving service collection.</param>
    /// <param name="legacyPrefixes">Explicitly retired credential namespaces.</param>
    /// <returns>The exact receiving target.</returns>
    /// <exception cref="InvalidVerifiedCallerInstallation">The configuration is incomplete, duplicated or contains forwarding without the composition opt-in.</exception>
    internal CallerCredentialTarget InstallPersonalAccessTokens(IServiceCollection services, IEnumerable<string> legacyPrefixes)
    {
        if (_allowPersonalAccessTokenCallers)
        {
            Install(services);
        }
        else
        {
            if (!VerifiedCallerInstallation.IsValid(_resource, _audience, _environment) || _directory is null || _forwarding is not null)
            {
                throw new InvalidVerifiedCallerInstallation("PAT callers require a resource, audience, environment and person directory, without signed forwarding configuration.");
            }
            RegisterInstallation(services);
            services.AddSingleton(new PatCapableCallerInstallation());
        }
        services.AddSingleton(new ReservedCredentialPrefixes(legacyPrefixes));
        return new(_resource!, _audience!, _environment!);
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
        services.Add(_directory!);
        services.AddVerifiedCallers();
    }

    [IgnoreConvention]
    sealed class InstallationMarker;
}
