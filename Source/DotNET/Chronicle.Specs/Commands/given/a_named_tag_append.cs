// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.given;

public abstract class a_named_tag_append : Specification
{
    protected IEventLog _eventLog;
    protected IUnitOfWork _unitOfWork;
    protected IEventTypes _eventTypes;
    protected IConcurrencyScopeStrategies _strategies;
    protected CommandContext _context;
    protected ICommandResponseValueHandler _handler;
    protected object _value;
    protected CommandResult _result;
    protected EventSourceId _id;
    protected NamedTag[] _contextTags;
    protected NamedTag[] _wrapperTags;
    protected readonly List<EventForEventSourceId> _appended = [];
    protected readonly List<EventForEventSourceId> _enrolled = [];
    protected readonly DateTimeOffset _occurred = new(2020, 1, 2, 3, 4, 5, TimeSpan.Zero);
    protected bool _transactional;
    protected bool _definition;
    protected bool _tagged = true;
    protected bool _subject;

    protected abstract ICommandResponseValueHandler CreateHandler();
    protected abstract object CreateValue();

    void Establish()
    {
        _id = EventSourceId.New();
        _contextTags = [new("project", "context"), new("shared", "same")];
        _wrapperTags = [new("project", "wrapper"), new("shared", "same")];
        _context = new(CorrelationId.New(), typeof(object), new object(), [], new() { { WellKnownCommandContextKeys.EventSourceId, _id } }, null);
        _eventLog = Substitute.For<EventLogWithNamedTags>();
        _eventLog.Id.Returns(EventSequenceId.Log);
        _unitOfWork = Substitute.For<UnitOfWorkWithNamedTags>();
        _eventTypes = Substitute.For<IEventTypes>();
        _eventTypes.HasFor(typeof(TestEvent)).Returns(true);
        _strategies = Substitute.For<IConcurrencyScopeStrategies>();
        var strategy = Substitute.For<IConcurrencyScopeStrategy>();
        strategy.StandInForAnOptimisticStrategy();
        _strategies.GetFor(_eventLog).Returns(strategy);

        _eventLog.AppendWithNamedTags(default!, default!, default!).ReturnsForAnyArgs(call =>
        {
            _appended.Add(new(call.ArgAt<EventSourceId>(0), call.ArgAt<object>(1)) { NamedTags = call.ArgAt<IEnumerable<NamedTag>>(2), Tags = call.ArgAt<IEnumerable<string>?>(7) ?? [], Occurred = call.ArgAt<DateTimeOffset?>(9), Subject = call.ArgAt<Subject?>(10) });
            return AppendResult.Success(_context.CorrelationId, EventSequenceNumber.First);
        });
        _eventLog.AppendManyWithNamedTags(default(EventSourceId)!, default!, default!).ReturnsForAnyArgs(call =>
        {
            _appended.AddRange(call.ArgAt<IEnumerable<object>>(1).Select(@event => new EventForEventSourceId(call.ArgAt<EventSourceId>(0), @event) { NamedTags = call.ArgAt<IEnumerable<NamedTag>>(2) }));
            return AppendManyResult.Success(_context.CorrelationId, []);
        });
        _eventLog.AppendManyWithNamedTags(default!, default!).ReturnsForAnyArgs(call =>
        {
            _appended.AddRange(call.ArgAt<IEnumerable<EventForEventSourceId>>(0).Select(@event => @event with
            {
                Tags = @event.Tags.Concat(call.ArgAt<IEnumerable<string>?>(3) ?? []).Distinct().ToArray()
            }));
            return AppendManyResult.Success(_context.CorrelationId, []);
        });
        _eventLog.Append(default!, default!).ReturnsForAnyArgs(call =>
        {
            _appended.Add(new(call.ArgAt<EventSourceId>(0), call.ArgAt<object>(1)));
            return AppendResult.Success(_context.CorrelationId, EventSequenceNumber.First);
        });
        _eventLog.AppendMany(default(EventSourceId)!, default!).ReturnsForAnyArgs(call =>
        {
            _appended.AddRange(call.ArgAt<IEnumerable<object>>(1).Select(@event => new EventForEventSourceId(call.ArgAt<EventSourceId>(0), @event)));
            return AppendManyResult.Success(_context.CorrelationId, []);
        });
        _eventLog.AppendMany(default!).ReturnsForAnyArgs(call =>
        {
            _appended.AddRange(call.ArgAt<IEnumerable<EventForEventSourceId>>(0));
            return AppendManyResult.Success(_context.CorrelationId, []);
        });
        _unitOfWork.WhenForAnyArgs(_ => _.AddEventWithNamedTags(default!, default!, default!, default!, default!)).Do(call =>
            _enrolled.Add(new(call.ArgAt<EventSourceId>(1), call.ArgAt<object>(2), call.ArgAt<Causation?>(4)) { NamedTags = call.ArgAt<IEnumerable<NamedTag>>(3), Tags = call.ArgAt<IEnumerable<string>?>(9) ?? [], Occurred = call.ArgAt<DateTimeOffset?>(10), Subject = call.ArgAt<Subject?>(11) }));
        _unitOfWork.WhenForAnyArgs(_ => _.AddEvent(default!, default!, default!, default!)).Do(call =>
            _enrolled.Add(new(call.ArgAt<EventSourceId>(1), call.ArgAt<object>(2), call.ArgAt<Causation?>(3))));
        _unitOfWork.WhenForAnyArgs(_ => _.AddEvents(default!, default!, default!)).Do(call =>
            _enrolled.AddRange(call.ArgAt<IEnumerable<EventForEventSourceId>>(1)));
    }

    protected async Task Handle()
    {
        if (_tagged)
        {
            _context.Values[WellKnownCommandContextKeys.EventTags] = _contextTags;
        }
        if (_definition)
        {
            _context.Values[WellKnownCommandContextKeys.EventSource] = typeof(TestSource);
            _context.Values[WellKnownCommandContextKeys.EventStream] = "changes";
        }
        if (_subject)
        {
            _context.Values[WellKnownCommandContextKeys.Subject] = new Subject("customer");
        }
        _handler = CreateHandler();
        _value = CreateValue();
        if (_transactional)
        {
            CommandTransaction.Current = _unitOfWork;
        }
        _result = await _handler.Handle(_context, _value);
    }

    protected EventForEventSourceId Wrapper() => new(_id, new TestEvent("wrapped"))
    {
        NamedTags = _tagged ? _wrapperTags : [],
        Tags = ["legacy"],
        Occurred = _occurred
    };

    void Destroy() => CommandTransaction.Current = null;

    public record TestEvent(string Name);

    [EventSource]
    [EventStream("changes")]
    public class TestSource : IEventSource;
}
