// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Streams.for_StreamReads.when_reading;

public class with_sentinel_routes : Specification
{
    readonly List<Exception?> _errors = [];
    IEventLog _log;
    StreamReads _reads;

    void Establish()
    {
        _log = Substitute.For<IEventLog>();
        _reads = new(_log, Substitute.For<ICommandContextAccessor>());
    }

    async Task Because()
    {
        var valid = new EventRoute("reports", "approval", "specific");
        (EventSourceId Id, EventRoute Route)[] invalid =
        [
            (EventSourceId.Unspecified, valid),
            (EventSourceId.New(), valid with { EventStreamType = EventStreamType.All }),
            (EventSourceId.New(), valid.WithStreamId(EventStreamId.Default)),
            (EventSourceId.New(), valid.WithStreamId(EventStreamId.NotSet))
        ];
        foreach (var (id, route) in invalid)
        {
            _errors.Add(await Catch.Exception(() => _reads.Stream(id, route)));
            _errors.Add(await Catch.Exception(() => _reads.StreamType(id, route)));
        }
    }

    [Fact] void should_refuse_every_sentinel_in_both_read_modes() => _errors.TrueForAll(error => error is StreamDecisionRequiresSpecificRoute).ShouldBeTrue();
    [Fact] void should_not_read_an_unnarrowed_tail() => _log.ReceivedCalls().ShouldBeEmpty();
}
