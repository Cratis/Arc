// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_an_assignable_event;

public class and_the_decision_is_unrelated : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => new given.ItemRemoved(3).ShouldBeEventAssignableTo<given.ItemAdded>());
    [Fact] void should_fail() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
}
