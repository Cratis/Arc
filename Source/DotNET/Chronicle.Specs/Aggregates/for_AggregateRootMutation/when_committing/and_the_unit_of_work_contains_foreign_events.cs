// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutation.when_committing;

/// <summary>
/// When the shared unit of work also carries events from elsewhere (for example a second aggregate), the commit
/// result must report only this aggregate's own events and must not fabricate sequence numbers it cannot attribute —
/// otherwise Events and SequenceNumbers no longer correspond one-to-one.
/// </summary>
public class and_the_unit_of_work_contains_foreign_events : given.an_aggregate_mutation
{
    [EventType]
    class AggregateEvent;

    [EventType]
    class ForeignEvent;

    AggregateEvent _aggregateEvent;
    AggregateRootCommitResult _result;

    async Task Establish()
    {
        _aggregateEvent = new AggregateEvent();
        await _mutation.Apply(_aggregateEvent);

        _unitOfWork.GetEvents().Returns(new object[] { _aggregateEvent, new ForeignEvent() }.ToImmutableList());
        _unitOfWork
            .TryGetLastCommittedEventSequenceNumber(out Arg.Any<EventSequenceNumber>())
            .Returns(call =>
            {
                call[0] = (EventSequenceNumber)10UL;
                return true;
            });
    }

    async Task Because() => _result = await _mutation.Commit();

    [Fact] void should_report_only_this_aggregates_events() => _result.Events.ShouldContainOnly([_aggregateEvent]);
    [Fact] void should_not_fabricate_sequence_numbers_it_cannot_attribute() => _result.SequenceNumbers.ShouldBeEmpty();
}
