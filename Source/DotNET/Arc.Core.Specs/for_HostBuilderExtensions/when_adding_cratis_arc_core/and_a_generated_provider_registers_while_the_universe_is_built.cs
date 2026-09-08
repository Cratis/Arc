// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

[Collection("MutatesTypeDiscovery")]
public class and_a_generated_provider_registers_while_the_universe_is_built : Specification
{
    ITypes _typesFromTheContainer;
    ITypes _typesFromInternals;
    IDerivedTypes _derivedTypesFromTheContainer;
    IDerivedTypes _derivedTypesFromInternals;
    int _registeredUniverses;
    int _registeredDerivedTypes;

    void Establish() => GeneratedTypeDiscoveryRegistry.Register(new TheProviderThatBringsInTheLateOne());

    void Because()
    {
        using var serviceProvider = new ServiceCollection()
            .AddCratisArcCore()
            .BuildServiceProvider();

        _typesFromTheContainer = serviceProvider.GetRequiredService<ITypes>();
        _registeredUniverses = serviceProvider.GetServices<ITypes>().Count();
        _typesFromInternals = Internals.Types;

        _derivedTypesFromTheContainer = serviceProvider.GetRequiredService<IDerivedTypes>();
        _registeredDerivedTypes = serviceProvider.GetServices<IDerivedTypes>().Count();
        _derivedTypesFromInternals = Internals.DerivedTypes;
    }

    [Fact] void should_expose_the_late_type_to_the_container() => _typesFromTheContainer.All.ShouldContain(TheLateProvider.OnlyTypeItReports);
    [Fact] void should_expose_the_late_type_internally() => _typesFromInternals.All.ShouldContain(TheLateProvider.OnlyTypeItReports);
    [Fact] void should_hold_the_universe_the_container_resolves() => _typesFromInternals.ShouldBeSame(_typesFromTheContainer);
    [Fact] void should_leave_the_container_with_one_universe() => _registeredUniverses.ShouldEqual(1);

    /// <summary>
    /// Compares references rather than contents: nothing in <c>AddCratisArcCore</c> touches <c>Types.Instance</c>
    /// any more, so whether that static holds the late type depends only on when some test first reads it, and a
    /// content comparison would pass or fail on test order.
    /// </summary>
    [Fact] void should_not_be_the_static_snapshot() => _typesFromInternals.ShouldNotBeSame(Cratis.Types.Types.Instance);
    [Fact] void should_expose_the_late_derived_type_to_the_container() => _derivedTypesFromTheContainer.IsDerivedType(TheLateProvider.OnlyDerivedTypeItReports).ShouldBeTrue();
    [Fact] void should_expose_the_late_derived_type_internally() => _derivedTypesFromInternals.IsDerivedType(TheLateProvider.OnlyDerivedTypeItReports).ShouldBeTrue();
    [Fact] void should_expose_the_late_target_type_as_having_derivatives() => _derivedTypesFromInternals.TypesWithDerivatives.ShouldContain(TheLateProvider.TargetOfTheOnlyDerivedTypeItReports);
    [Fact] void should_hold_the_derived_types_the_container_resolves() => _derivedTypesFromInternals.ShouldBeSame(_derivedTypesFromTheContainer);
    [Fact] void should_leave_the_container_with_one_set_of_derived_types() => _registeredDerivedTypes.ShouldEqual(1);

    /// <summary>
    /// Compares references for the same reason <c>should_not_be_the_static_snapshot</c> does, and reading
    /// <see cref="DerivedTypes.Instance"/> is what would pin <c>Types.Instance</c> early if production code did it.
    /// </summary>
    [Fact] void should_not_be_the_static_derived_types_snapshot() => _derivedTypesFromInternals.ShouldNotBeSame(DerivedTypes.Instance);
}
