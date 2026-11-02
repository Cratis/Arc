// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_a_contained_event;

public class and_several_match : Specification
{
    given.ItemAdded _first = new(3);
    given.ItemAdded _matched;
    void Because() => _matched = new[] { _first, new given.ItemAdded(4) }.ShouldContainEvent<given.ItemAdded>(_ => _.Quantity > 0);
    [Fact] void should_return_the_first_match() => _matched.ShouldEqual(_first);
}
