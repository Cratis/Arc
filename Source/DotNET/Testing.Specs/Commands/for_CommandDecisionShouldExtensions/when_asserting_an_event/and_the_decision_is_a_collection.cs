// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_an_event;

public class and_the_decision_is_a_collection : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => new[] { new given.ItemAdded(3) }.ShouldBeEvent<given.ItemAdded>());
    [Fact] void should_not_treat_a_collection_as_one_event() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_suggest_containment() => _error.Message.ShouldContain("use ShouldContainEvent");
}
