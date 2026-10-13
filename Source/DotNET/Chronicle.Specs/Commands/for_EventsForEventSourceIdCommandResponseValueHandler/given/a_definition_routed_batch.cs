// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.given;

public class a_definition_routed_batch : an_events_for_event_source_id_command_response_value_handler
{
    protected object[] _value;
    protected readonly List<EventForEventSourceId> _singleAppendRoutes = [];
    protected EventSourceId _id;

    void Establish()
    {
        _id = EventSourceId.New();
        _commandContext.Values[WellKnownCommandContextKeys.EventSourceId] = _id;
        _commandContext.Values[WellKnownCommandContextKeys.EventSource] = typeof(AccountSource);
        _commandContext.Values[WellKnownCommandContextKeys.EventStream] = "Transactions";
        _commandContext.Values[WellKnownCommandContextKeys.EventSourceType] = new EventSourceType("Account");
        _commandContext.Values[WellKnownCommandContextKeys.EventStreamType] = new EventStreamType("Transactions");
        _commandContext.Values[WellKnownCommandContextKeys.EventStreamId] = new EventStreamId("account-stream");
        _commandContext.Values[WellKnownCommandContextKeys.Subject] = new Subject("account-subject");
    }

    protected async Task CaptureSingleAppendRoutes()
    {
        var singleAppendLog = Substitute.For<EventLogWithNamedTags>();
        singleAppendLog.AppendThroughEventSource(default!, default!, default!).ReturnsForAnyArgs(call =>
        {
            _singleAppendRoutes.Add(new(call.ArgAt<EventSourceId>(1), call.ArgAt<object>(2))
            {
                EventSource = call.ArgAt<Type>(0),
                EventStream = call.ArgAt<string?>(3),
                EventStreamId = call.ArgAt<EventStreamId?>(4) ?? EventStreamId.Default,
                Tags = call.ArgAt<IEnumerable<string>?>(6) ?? [],
                Occurred = call.ArgAt<DateTimeOffset?>(8),
                Subject = call.ArgAt<Subject?>(9)
            });
            return AppendResult.Success(_correlationId, EventSequenceNumber.First);
        });
        foreach (var item in _value)
        {
            var wrapper = item as EventForEventSourceId;
            var routing = CommandTransactionAppender.ResolveRouting(wrapper, _commandContext, _eventSources);
            await singleAppendLog.AppendForCommand(wrapper?.EventSourceId ?? _id, wrapper?.Event ?? item, routing, null, wrapper?.SuppliedTags(), wrapper?.Occurred);
        }
    }

    public class AccountSource;
    public class LedgerSource;
}
