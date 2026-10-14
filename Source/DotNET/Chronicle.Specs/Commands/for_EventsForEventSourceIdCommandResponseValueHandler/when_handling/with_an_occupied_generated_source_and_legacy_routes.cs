// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_an_occupied_generated_source_and_legacy_routes : given.a_mixed_generated_batch
{
    async Task Establish() => await SeedOccupiedSource();
    async Task Because() => await Execute();

    [Fact] void should_reject_the_command() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_classify_the_collision() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_name_the_collision() => _result.ValidationResults.Single().ReasonDetail.ShouldEqual("GeneratedIdentifierCollision");
    [Fact] void should_append_no_generated_events() => _stored.Select(@event => ((Created)@event.Content).Name).ShouldContainOnly("existing");
}
