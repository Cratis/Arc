// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_executing;

public class with_stream_id_concurrency_on_an_occupied_source : given.an_attributed_generated_source<given.attributed_source_commands.WithStreamId>
{
    async Task Establish()
    {
        _command.Shape = given.a_fresh_identifier_scenario.ResultShape.PlainEvents;
        await SeedOccupiedSource();
    }
    async Task Because() => await Execute();

    [Fact] void should_classify_the_collision() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_name_the_collision() => _result.ValidationResults.Single().ReasonDetail.ShouldEqual("GeneratedIdentifierCollision");
    [Fact] void should_append_nothing() => _tailAfter.ShouldEqual(_tailBefore);
    [Fact] void should_invoke_handle_once() => _command.Calls.ShouldEqual(1);
}
