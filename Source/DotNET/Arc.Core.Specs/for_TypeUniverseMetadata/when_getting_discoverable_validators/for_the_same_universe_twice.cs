// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.for_TypeUniverseMetadata.when_getting_discoverable_validators;

public class for_the_same_universe_twice : Specification
{
    ITypes _types;
    IReadOnlySet<Type> _first;
    IReadOnlySet<Type> _second;

    void Establish()
    {
        _types = Substitute.For<ITypes>();
        _types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(string)]);
    }

    void Because()
    {
        _first = TypeUniverseMetadata.DiscoverableValidatorsIn(_types);
        _second = TypeUniverseMetadata.DiscoverableValidatorsIn(_types);
    }

    [Fact] void should_hold_the_validators_the_universe_reports() => _first.ShouldContainOnly([typeof(string)]);
    [Fact] void should_reuse_the_validator_types() => _second.ShouldBeSame(_first);
    [Fact] void should_ask_the_universe_once() => _types.Received(1).FindMultiple(typeof(IDiscoverableValidator<>));
}
