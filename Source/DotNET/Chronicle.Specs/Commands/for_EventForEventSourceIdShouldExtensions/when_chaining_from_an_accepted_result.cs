// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Commands.for_EventForEventSourceIdShouldExtensions;

public class when_chaining_from_an_accepted_result : Specification
{
    given.ItemAdded _matched;
    void Because()
    {
        var result = Result<IEnumerable<EventForEventSourceId>, ValidationResult>.Success([new("first", new given.ItemAdded(2))]);
        _matched = result.ShouldBeAccepted().ShouldContainEvent<given.ItemAdded>("first");
    }
    [Fact] void should_return_the_matching_payload() => _matched.Quantity.ShouldEqual(2);
}
