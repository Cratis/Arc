// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_an_event;

public class and_the_decision_is_null : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => ((object)null!).ShouldBeEvent<given.ItemAdded>());
    [Fact] void should_fail() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_identify_null() => _error.Message.ShouldContain("null");
}
