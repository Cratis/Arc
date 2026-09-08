// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

[Collection("MutatesTypeDiscovery")]
public class it_should_hold_every_type_the_registered_providers_report : Specification
{
    Type[] _typesTheProvidersReport;
    Type[] _typesTheUniverseIsMissing;

    /// <summary>
    /// Compares the universe against the registry rather than against a named type, so it holds whatever else has
    /// registered a provider by the time it runs.
    /// </summary>
    /// <remarks>
    /// In this assembly it passes on very little - nothing here arrives through the assembly closure walk, which
    /// is what the separate Arc.Core.TypeDiscovery.Specs process exists to cover. It costs one set difference and
    /// catches the whole class of regression in any host: a universe taken before a provider registered is missing
    /// that provider's types, and the registry is the only thing that can say so.
    /// </remarks>
    void Because()
    {
        using var serviceProvider = new ServiceCollection()
            .AddCratisArcCore()
            .BuildServiceProvider();

        _typesTheProvidersReport = [.. GeneratedTypeDiscoveryRegistry.Providers.SelectMany(_ => _.DefinedTypes).Distinct()];
        _typesTheUniverseIsMissing = [.. _typesTheProvidersReport.Except(Internals.Types.All)];
    }

    [Fact] void should_have_been_given_something_to_check() => _typesTheProvidersReport.ShouldNotBeEmpty();
    [Fact] void should_be_missing_none_of_them() => _typesTheUniverseIsMissing.ShouldBeEmpty();
}
