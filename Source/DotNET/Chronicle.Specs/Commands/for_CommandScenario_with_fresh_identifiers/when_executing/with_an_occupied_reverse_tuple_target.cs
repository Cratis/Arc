// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_executing;

public class with_an_occupied_reverse_tuple_target : given.a_fresh_identifier_scenario
{
    async Task Establish()
    {
        _command = new(ResultShape.ReverseTuple);
        await SeedOccupiedSource();
    }

    async Task Because() => await Execute();

    [Fact] void should_classify_the_collision() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_name_generated_identifier_collision() => _result.ValidationResults.Single().ReasonDetail.ShouldEqual("GeneratedIdentifierCollision");
    [Fact] void should_append_nothing() => _tailAfter.ShouldEqual(_tailBefore);
    [Fact] void should_invoke_handle_only_once() => _command.Calls.ShouldEqual(1);
}
