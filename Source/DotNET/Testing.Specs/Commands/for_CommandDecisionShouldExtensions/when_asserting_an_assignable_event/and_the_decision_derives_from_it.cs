// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_an_assignable_event;

public class and_the_decision_derives_from_it : Specification
{
    given.SpecialItemAdded _event = new(3);
    given.ItemAdded _matched;
    void Because() => _matched = _event.ShouldBeEventAssignableTo<given.ItemAdded>();
    [Fact] void should_return_the_derived_event() => _matched.ShouldEqual(_event);
}
