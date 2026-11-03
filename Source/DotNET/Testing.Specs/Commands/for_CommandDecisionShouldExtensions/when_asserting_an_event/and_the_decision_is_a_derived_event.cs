// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_an_event;

public class and_the_decision_is_a_derived_event : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => new given.SpecialItemAdded(3).ShouldBeEvent<given.ItemAdded>());
    [Fact] void should_require_the_exact_type() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_describe_the_derived_event() => _error.Message.ShouldContain("SpecialItemAdded");
}
