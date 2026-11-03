// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_a_contained_event;

public class and_the_collection_is_empty : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => Array.Empty<object>().ShouldContainEvent<given.ItemAdded>());
    [Fact] void should_fail() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_describe_the_empty_collection() => _error.Message.ShouldContain("no values");
}
