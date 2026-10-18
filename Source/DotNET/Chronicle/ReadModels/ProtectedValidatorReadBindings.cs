// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Contributes Chronicle-owned read origins and direct verification without injectable trust markers.
/// </summary>
internal static class ProtectedValidatorReadBindings
{
    /// <summary>
    /// Records reader descriptors inserted during Arc's Chronicle registration call, by reference identity.
    /// </summary>
    /// <param name="services">The original collection after the call.</param>
    /// <param name="before">Descriptor identities immediately before the call.</param>
    /// <param name="applicationBindings">Descriptors added during application callbacks, which cannot gain client origin.</param>
    internal static void RecordClientReaders(IServiceCollection services, IReadOnlySet<ServiceDescriptor> before, IReadOnlySet<ServiceDescriptor> applicationBindings)
    {
        var origins = ProtectedDecisionRegistrationOrigins.For(services);
        foreach (var descriptor in services.Where(binding => binding.ServiceType == typeof(IDecisionReads) && !before.Contains(binding) && !applicationBindings.Contains(binding)))
        {
            origins.Record(descriptor, new(IsChronicleClientReader: true));
        }
    }

    /// <summary>
    /// Records the exact command-aware descriptor just inserted by the framework.
    /// </summary>
    /// <param name="services">The original collection.</param>
    /// <param name="descriptor">The descriptor actually inserted.</param>
    internal static void RecordReader(IServiceCollection services, ServiceDescriptor descriptor)
    {
        var origins = ProtectedDecisionRegistrationOrigins.For(services);
        origins.RecognizeReaders(typeof(IDecisionReads), [typeof(IReadModels), typeof(IEventStore), typeof(IEventLog), typeof(DecisionRead<>)]);
        foreach (var previous in services.Where(binding => binding.ServiceType == typeof(IDecisionReads) && !binding.IsKeyedService))
        {
            if (origins.IsConvention(previous) && previous.ImplementationType is { } implementation &&
                implementation.Assembly == typeof(IDecisionReads).Assembly)
            {
                origins.Record(previous, new(IsSupersededReader: true));
            }
        }
        origins.Record(descriptor, new(VerifyRead: VerifyReader, IsCommandReader: true));
    }

    /// <summary>
    /// Records a token factory only when the framework actually inserted it.
    /// </summary>
    /// <param name="services">The original collection.</param>
    /// <param name="descriptor">The inserted token factory.</param>
    /// <param name="models">The discovered plain read models, which are not eligible dependencies.</param>
    internal static void RecordToken(IServiceCollection services, ServiceDescriptor descriptor, IEnumerable<Type> models)
    {
        var origins = ProtectedDecisionRegistrationOrigins.For(services);
        origins.RecognizeReaders(typeof(IDecisionReads), models);
        origins.Record(descriptor, new(VerifyRead: VerifyToken));
    }

    static void VerifyReader(object? value)
    {
        if (value is not CommandDecisionReads)
        {
            throw new ProtectedCommandRequiresCommandAwareReader();
        }
    }

    static void VerifyToken(object? value)
    {
        if (value is null)
        {
            throw new ProtectedCommandRequiresIssuedDecisionRead();
        }
        CommandDecisionReads.VerifyProvided(value);
    }
}
