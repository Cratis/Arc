// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Arc.for_TypeUniverseMetadata.when_getting_derived_types;

public class for_the_same_universe_twice : Specification
{
    ITypes _types;
    DerivedTypes _first;
    DerivedTypes _second;

    void Establish()
    {
        _types = Substitute.For<ITypes>();
        _types.All.Returns([]);
    }

    void Because()
    {
        _first = TypeUniverseMetadata.DerivedTypesFor(_types);
        _second = TypeUniverseMetadata.DerivedTypesFor(_types);
    }

    [Fact] void should_reuse_the_derived_types() => _second.ShouldBeSame(_first);
    [Fact] void should_read_the_universe_once() => _ = _types.Received(1).All;
}
