// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection;

/// <summary>
/// Opts Microsoft DI into fresh, provider-bound construction of protected decision validators.
/// </summary>
public static class ProtectedDecisionServiceCollectionExtensions
{
    /// <summary>
    /// Builds the root through Arc's native-provider factory. Execute protected validation in a child scope.
    /// </summary>
    /// <param name="services">The original service collection, including Arc and Chronicle registrations.</param>
    /// <param name="options">Microsoft DI options, copied by the factory.</param>
    /// <returns>The native Microsoft root provider, owned by the caller.</returns>
    public static IServiceProvider BuildProtectedDecisionServiceProvider(this IServiceCollection services, ServiceProviderOptions? options = null)
    {
        var factory = new ProtectedDecisionServiceProviderFactory(options);
        return factory.CreateServiceProvider(factory.CreateBuilder(services));
    }

    /// <summary>
    /// Registers fresh Arc construction followed by configuration of that exact validator instance.
    /// </summary>
    /// <typeparam name="TValidator">The discoverable validator to configure.</typeparam>
    /// <param name="services">The original service collection.</param>
    /// <param name="configure">Trusted application code run once after construction and before certification.</param>
    /// <returns>The service collection for continuation.</returns>
    /// <remarks>
    /// Removes at most one Arc convention binding, never an application registration. The protected path requires
    /// an attested native scope, complete inspectable transient dependencies and an untrimmed JIT runtime.
    /// Ordinary DI resolution cannot construct this registration during a protected invocation.
    /// </remarks>
    public static IServiceCollection AddProtectedDecisionValidator<TValidator>(this IServiceCollection services, Action<TValidator> configure)
        where TValidator : class, IValidator
    {
        var origins = ProtectedDecisionRegistrationOrigins.For(services);
        var convention = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(TValidator) && origins.IsConvention(descriptor));
        if (convention is not null)
        {
            services.Remove(convention);
        }
        var descriptor = ServiceDescriptor.Transient(typeof(TValidator), provider => Construct(provider, configure));
        services.Add(descriptor);
        origins.Record(descriptor, new(Configure: validator => configure((TValidator)validator)));
        return services;
    }

    static object Construct<TValidator>(IServiceProvider provider, Action<TValidator> configure)
        where TValidator : class, IValidator
    {
        if (CommandDecisionPolicy.RefusesDiscoverableValidators || CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On)
        {
            throw DiscoverableValidatorRefusedInProtectedDecision.NotConstructedByArc(typeof(TValidator));
        }
        var validator = (TValidator)DiscoverableValidators.ConstructDependencies(provider, typeof(TValidator));
        try
        {
            configure(validator);
            return validator;
        }
        catch
        {
            ProtectedValidatorActivationFrame.DisposeOwned(validator).AsTask().GetAwaiter().GetResult();
            throw;
        }
    }
}
