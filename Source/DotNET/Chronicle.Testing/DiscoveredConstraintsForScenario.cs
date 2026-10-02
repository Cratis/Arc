// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Testing;
using Cratis.Serialization;

namespace Cratis.Arc.Chronicle.Testing;

/// <summary>
/// Discovers the same constraints as Chronicle's default event scenario for a scenario with an explicit identity.
/// </summary>
/// <param name="defaults">The defaults used for artifact and event type discovery.</param>
internal sealed class DiscoveredConstraintsForScenario(Defaults defaults) : ICanProvideConstraints
{
    /// <inheritdoc/>
    public IImmutableList<IConstraintDefinition> Provide()
    {
        // Chronicle's explicit-identity constructor does not discover constraints, and its discovery helper is private.
        var namingPolicy = new CamelCaseNamingPolicy();
        using var serviceProvider = new DefaultServiceProvider();
        using var loggerFactory = new NullLoggerFactory();
        var artifactActivator = new ClientArtifactsActivator(serviceProvider, loggerFactory);
        ICanProvideConstraints[] providers =
        [
            new ConstraintsByBuilderProvider(
                defaults.ClientArtifactsProvider,
                defaults.EventTypes,
                namingPolicy,
                artifactActivator,
                NullLogger<ConstraintsByBuilderProvider>.Instance),
            new UniqueConstraintProvider(
                defaults.ClientArtifactsProvider,
                defaults.EventTypes,
                namingPolicy),
            new UniqueEventTypeConstraintsProvider(
                defaults.ClientArtifactsProvider,
                defaults.EventTypes)
        ];

        return providers.SelectMany(provider => provider.Provide()).ToImmutableList();
    }
}
