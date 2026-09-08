// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

[Collection("MutatesTypeDiscovery")]
public class and_a_generated_provider_registers_while_the_universe_is_built : Specification
{
    ITypes _typesFromTheContainer;
    ITypes _typesFromInternals;
    int _registeredUniverses;

    void Establish() => GeneratedTypeDiscoveryRegistry.Register(new TheProviderThatBringsInTheLateOne());

    void Because()
    {
        using var serviceProvider = new ServiceCollection()
            .AddCratisArcCore()
            .BuildServiceProvider();

        _typesFromTheContainer = serviceProvider.GetRequiredService<ITypes>();
        _registeredUniverses = serviceProvider.GetServices<ITypes>().Count();
        _typesFromInternals = Internals.Types;
    }

    [Fact] void should_expose_the_late_type_to_the_container() => _typesFromTheContainer.All.ShouldContain(TheLateProvider.OnlyTypeItReports);
    [Fact] void should_expose_the_late_type_internally() => _typesFromInternals.All.ShouldContain(TheLateProvider.OnlyTypeItReports);
    [Fact] void should_hold_the_universe_the_container_resolves() => _typesFromInternals.ShouldBeSame(_typesFromTheContainer);
    [Fact] void should_leave_the_container_with_one_universe() => _registeredUniverses.ShouldEqual(1);
    [Fact] void should_not_be_the_static_snapshot() => Cratis.Types.Types.Instance.All.ShouldNotContain(TheLateProvider.OnlyTypeItReports);
}
