// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_a_contained_event;

public class and_only_a_derived_event_is_present : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => new[] { new given.SpecialItemAdded(3) }.ShouldContainEvent<given.ItemAdded>());
    [Fact] void should_require_an_exact_match() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
}
