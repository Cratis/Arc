// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

[Collection("MutatesTypeDiscovery")]
public class twice_for_the_same_universe : Specification
{
    ServiceProvider _first;
    ServiceProvider _second;
    IDerivedTypes _derivedTypesFromTheFirst;
    IDerivedTypes _derivedTypesFromTheSecond;
    IDerivedTypes _derivedTypesFromInternals;
    ITypes _typesFromTheFirst;
    ITypes _typesFromTheSecond;

    void Because()
    {
        _first = new ServiceCollection().AddCratisArcCore().BuildServiceProvider();
        _second = new ServiceCollection().AddCratisArcCore().BuildServiceProvider();
        _derivedTypesFromInternals = Internals.DerivedTypes;

        _typesFromTheFirst = _first.GetRequiredService<ITypes>();
        _typesFromTheSecond = _second.GetRequiredService<ITypes>();
        _derivedTypesFromTheFirst = _first.GetRequiredService<IDerivedTypes>();
        _derivedTypesFromTheSecond = _second.GetRequiredService<IDerivedTypes>();
    }

    [Fact] void should_hold_the_same_universe() => _typesFromTheSecond.ShouldBeSame(_typesFromTheFirst);
    [Fact] void should_reuse_the_derived_types() => _derivedTypesFromTheSecond.ShouldBeSame(_derivedTypesFromTheFirst);
    [Fact] void should_hold_the_derived_types_the_container_resolves() => _derivedTypesFromInternals.ShouldBeSame(_derivedTypesFromTheSecond);

    void Destroy()
    {
        _first.Dispose();
        _second.Dispose();
    }
}
