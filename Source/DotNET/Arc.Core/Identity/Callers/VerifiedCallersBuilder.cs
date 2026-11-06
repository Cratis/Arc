// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.ForwardedIdentityV2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Identity.Callers;

/// <summary>Configures one receiving resource and its signed browser-hop trust boundary.</summary>
public sealed class VerifiedCallersBuilder
{
    string? _resource;
    string? _audience;
    string? _environment;
    ServiceDescriptor? _directory;
    SignedForwardedIdentityV2Builder? _forwarding;

    /// <summary>Binds the exact resource, audience and environment without normalization.</summary>
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

    /// <summary>Selects the persisted-person directory, resolved in the request scope.</summary>
    /// <typeparam name="TDirectory">The product-owned directory implementation.</typeparam>
    /// <returns>This builder.</returns>
    public VerifiedCallersBuilder UsePersonDirectory<TDirectory>()
        where TDirectory : class, ICallerPersonDirectory
    {
        _directory = ServiceDescriptor.Scoped<ICallerPersonDirectory, TDirectory>();
        return this;
    }

    /// <summary>Configures the v2-only signed browser-hop verifier.</summary>
    /// <param name="configure">The explicit trust configuration.</param>
    /// <returns>This builder.</returns>
    public VerifiedCallersBuilder UseSignedForwardedIdentityV2(Action<SignedForwardedIdentityV2Builder> configure)
    {
        var forwarding = new SignedForwardedIdentityV2Builder();
        configure(forwarding);
        _forwarding = forwarding;
        return this;
    }

    /// <summary>Validates and atomically registers the installation configuration.</summary>
    /// <param name="services">The receiving service collection.</param>
    /// <exception cref="InvalidOperationException">The configuration is incomplete or already installed.</exception>
    internal void Install(IServiceCollection services)
    {
        if (!VerifiedCallerInstallation.IsValid(_resource, _audience, _environment) || _directory is null || _forwarding is null)
        {
            throw new InvalidOperationException("Verified callers require a resource, audience, environment, person directory and signed forwarding configuration.");
        }
        var settings = _forwarding.Settings(_resource!, _audience!);
        if (services.Any(descriptor => descriptor.ServiceType == typeof(ForwardedIdentityV2Settings) ||
            (descriptor.ServiceType == typeof(VerifiedCallerInstallation) && descriptor.ImplementationInstance is not VerifiedCallerInstallation { Resource: null, Environment: null })))
        {
            throw new InvalidOperationException("Verified callers already have an installation.");
        }
        services.RemoveAll<VerifiedCallerInstallation>();
        services.RemoveAll<CallerResourceCatalog>();
        services.AddSingleton(new VerifiedCallerInstallation(_resource, _environment));
        services.AddSingleton(new CallerResourceCatalog(new Dictionary<string, string>(StringComparer.Ordinal) { [_resource!] = _audience! }));
        services.Add(_directory);
        _forwarding.InstallNonceStore(services);
        services.AddVerifiedCallers();
        services.AddForwardedIdentityV2(settings);
    }
}
