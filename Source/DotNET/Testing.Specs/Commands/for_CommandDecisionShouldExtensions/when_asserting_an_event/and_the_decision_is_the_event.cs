// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_an_event;

public class and_the_decision_is_the_event : Specification
{
    given.ItemAdded _event = new(2);
    given.ItemAdded _matched;
    void Because() => _matched = _event.ShouldBeEvent<given.ItemAdded>();
    [Fact] void should_return_the_event() => _matched.ShouldEqual(_event);
}
