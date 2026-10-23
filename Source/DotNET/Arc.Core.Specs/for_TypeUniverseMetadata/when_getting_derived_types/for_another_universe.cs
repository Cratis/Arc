// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Arc.for_TypeUniverseMetadata.when_getting_derived_types;

public class for_another_universe : Specification
{
    ITypes _firstUniverse;
    ITypes _secondUniverse;
    DerivedTypes _first;
    DerivedTypes _second;

    void Establish()
    {
        _firstUniverse = Substitute.For<ITypes>();
        _firstUniverse.All.Returns([]);
        _secondUniverse = Substitute.For<ITypes>();
        _secondUniverse.All.Returns([typeof(TheDerivedType)]);
        _first = DerivedTypes.For(_firstUniverse);
    }

    void Because() => _second = DerivedTypes.For(_secondUniverse);

    [Fact] void should_build_new_derived_types() => ReferenceEquals(_second, _first).ShouldBeFalse();
    [Fact] void should_read_them_off_the_other_universe() => _second.IsDerivedType(typeof(TheDerivedType)).ShouldBeTrue();
    [Fact] void should_leave_the_first_universe_without_them() => _first.IsDerivedType(typeof(TheDerivedType)).ShouldBeFalse();

    interface ITheTarget;

    [DerivedType("7a1d2c3e-4b5f-4e6a-8c9d-0e1f2a3b4c5d")]
    class TheDerivedType : ITheTarget;
}
