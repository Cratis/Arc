// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_EventsCommandResponseValueHandler.when_handling;

public class with_a_generated_target_collision_without_a_transaction : Commands.given.a_generated_target_collision
{
    async Task Because() => _result = await new EventsCommandResponseValueHandler(_eventLog, _eventTypes, _strategies).Handle(_context, new object[] { new Created("first"), new Created("second") });

    [Fact] void should_guard_the_append() => _scope.SequenceNumber.ShouldEqual(EventSequenceNumber.BeforeFirst);
    [Fact] void should_guard_the_whole_source() => _scope.EventSourceId.ShouldEqual(_id);
    [Fact] void should_classify_the_collision() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_identify_generated_provenance() => _result.ValidationResults.Single().ReasonDetail.ShouldEqual("GeneratedIdentifierCollision");
}
