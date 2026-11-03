// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Monads;

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_a_contained_event;

public class and_the_decision_is_an_accepted_result_of_a_tuple : Specification
{
    given.ItemAdded _event = new(3);
    given.ItemAdded _matched;
    void Because()
    {
        Result<(Guid, given.ItemAdded), ValidationResult> result = (Guid.Empty, _event);
        _matched = result.ShouldContainEvent<given.ItemAdded>();
    }
    [Fact] void should_search_the_active_tuple() => _matched.ShouldEqual(_event);
}
