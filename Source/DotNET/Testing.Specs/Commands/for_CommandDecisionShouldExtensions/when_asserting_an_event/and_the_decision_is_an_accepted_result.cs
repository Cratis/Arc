// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Monads;

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_an_event;

public class and_the_decision_is_an_accepted_result : Specification
{
    given.ItemAdded _event = new(3);
    given.ItemAdded _matched;
    void Because()
    {
        Result<given.ItemAdded, ValidationResult> result = _event;
        _matched = result.ShouldBeEvent<given.ItemAdded>();
    }
    [Fact] void should_unwrap_the_active_value() => _matched.ShouldEqual(_event);
}
