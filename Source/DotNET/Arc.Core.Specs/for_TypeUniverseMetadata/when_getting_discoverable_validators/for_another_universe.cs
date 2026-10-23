// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.for_TypeUniverseMetadata.when_getting_discoverable_validators;

public class for_another_universe : Specification
{
    ITypes _secondUniverse;
    IReadOnlySet<Type> _first;
    IReadOnlySet<Type> _second;

    void Establish()
    {
        var firstUniverse = Substitute.For<ITypes>();
        firstUniverse.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(string)]);
        _secondUniverse = Substitute.For<ITypes>();
        _secondUniverse.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(int)]);
        _first = TypeUniverseMetadata.DiscoverableValidatorsIn(firstUniverse);
    }

    void Because() => _second = TypeUniverseMetadata.DiscoverableValidatorsIn(_secondUniverse);

    [Fact] void should_find_new_validator_types() => ReferenceEquals(_second, _first).ShouldBeFalse();
    [Fact] void should_hold_the_validators_the_other_universe_reports() => _second.ShouldContainOnly([typeof(int)]);
}
