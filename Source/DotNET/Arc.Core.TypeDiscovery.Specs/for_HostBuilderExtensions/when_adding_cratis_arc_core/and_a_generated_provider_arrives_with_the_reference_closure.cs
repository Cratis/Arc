// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.TypeDiscovery.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

public class and_a_generated_provider_arrives_with_the_reference_closure : Specification
{
    ITypes _typesFromInternals;
    ITypes _typesFromTheContainer;
    ITypes _typesFromASecondContainer;

    /// <summary>
    /// Names no plugin type, so nothing forces the plugin's module initializer before
    /// <c>AddCratisArcCore</c> reaches the assembly closure walk. The facts run afterwards and are free to.
    /// </summary>
    void Because()
    {
        using var serviceProvider = new ServiceCollection()
            .AddCratisArcCore()
            .BuildServiceProvider();

        _typesFromTheContainer = serviceProvider.GetRequiredService<ITypes>();
        _typesFromInternals = Internals.Types;

        using var secondServiceProvider = new ServiceCollection()
            .AddCratisArcCore()
            .BuildServiceProvider();

        _typesFromASecondContainer = secondServiceProvider.GetRequiredService<ITypes>();
    }

    [Fact] void should_discover_the_plugins_marker_type() => _typesFromInternals.All.ShouldContain(typeof(PluginMarker));
    [Fact] void should_find_the_plugins_marker_type_through_its_contract() => _typesFromInternals.FindMultiple<IPluginMarker>().ShouldContain(typeof(PluginMarker));
    [Fact] void should_hold_the_universe_the_container_resolves() => _typesFromInternals.ShouldBeSame(_typesFromTheContainer);
    [Fact] void should_hand_a_second_container_the_same_universe() => _typesFromASecondContainer.ShouldBeSame(_typesFromInternals);
}
