// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_an_event;

public class and_the_predicate_throws : Specification
{
    InvalidOperationException _expected = new("predicate failed");
    Exception _error;
    void Because() => _error = Catch.Exception(() => new given.ItemAdded(3).ShouldBeEvent<given.ItemAdded>(_ => throw _expected));
    [Fact] void should_propagate_the_exception() => _error.ShouldEqual(_expected);
}
