// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection;

/// <summary>
/// Builds a Microsoft dependency injection provider whose native child scopes can be attested by Arc.
/// </summary>
/// <param name="options">The provider options, copied when the factory is created.</param>
/// <remarks>
/// Attestation identifies a provider and its final registrations; it does not certify application code.
/// The root provider itself is not eligible for protected decision scope attestation.
/// </remarks>
public sealed class ProtectedDecisionServiceProviderFactory(ServiceProviderOptions? options = null) : IServiceProviderFactory<IServiceCollection>
{
    readonly bool _validateScopes = options?.ValidateScopes ?? false;
    readonly bool _validateOnBuild = options?.ValidateOnBuild ?? false;

    /// <inheritdoc/>
    public IServiceCollection CreateBuilder(IServiceCollection services) => services;

    /// <inheritdoc/>
    public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
    {
        IServiceCollection services = new ServiceCollection();
        foreach (var descriptor in containerBuilder)
        {
            services.Add(descriptor);
        }
        services.AddSingleton(_ => new ProtectedDecisionProviderLifetime());
        services.AddScoped(ProtectedDecisionProviderAttestations.CreateProbe);
        var snapshot = Array.AsReadOnly(services.ToArray());
        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = _validateScopes,
            ValidateOnBuild = _validateOnBuild
        });
        try
        {
            var manifest = ProtectedDecisionRegistrationOrigins.For(containerBuilder).Snapshot(snapshot);
            ProtectedDecisionProviderAttestations.TrackRoot(provider, snapshot, manifest);
            return provider;
        }
        catch
        {
            provider.Dispose();
            throw;
        }
    }
}
