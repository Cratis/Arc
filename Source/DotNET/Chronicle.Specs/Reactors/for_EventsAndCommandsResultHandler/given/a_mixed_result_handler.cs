// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors.for_EventsAndCommandsResultHandler.given;

public class a_mixed_result_handler : Specification
{
    protected EventsAndCommandsResultHandler _handler;
    protected ICommandSideEffectExecutor _executor;
    protected IEventStore _store;
    protected IEventLog _eventLog;
    protected ReactorContext _context;
    protected IReactorSideEffectHandler[] _existingHandlers;
    protected ReactorSideEffectHandlers _dispatcher;
    protected Result<ReactorSideEffectFailure> _result;
    protected IEnumerable<EventForEventSourceId> _appended = [];
    protected IEnumerable<object> _executed = [];
    protected List<string> _order = [];

    void Establish()
    {
        _executor = Substitute.For<ICommandSideEffectExecutor>();
        _store = Substitute.For<IEventStore>();
        _eventLog = Substitute.For<IEventLog>();
        _store.EventLog.Returns(_eventLog);
        _store.EventTypes.HasFor(typeof(TestEvent)).Returns(true);
        _store.EventTypes.HasFor(typeof(AmbiguousCommand)).Returns(true);
        _context = new(EventContext.Empty with { EventSourceId = "observed" }, new TestReactor(), new()) { EventStore = _store };
        _handler = new(_executor);
        _existingHandlers =
        [
            new EventResultHandler(), new EventsResultHandler(), new EventForEventSourceIdResultHandler(),
            new EventsForEventSourceIdResultHandler(), new MixedSideEffectsResultHandler(), new EventsWithConcurrencyScopesResultHandler(),
            new CommandResultHandler(_executor), new CommandsResultHandler(_executor), new CompleteStreamResultHandler()
        ];
        var instances = Substitute.For<IInstancesOf<IReactorSideEffectHandler>>();
        instances.GetEnumerator().Returns(_ => _existingHandlers.Append(_handler).GetEnumerator());
        _dispatcher = new(instances);
        _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>()).Returns(call =>
        {
            _appended = call.Arg<IEnumerable<EventForEventSourceId>>().ToArray();
            _order.Add("append");
            return AppendManyResult.Success(Cratis.Execution.CorrelationId.New(), [EventSequenceNumber.First]);
        });
        _executor.Execute(Arg.Any<IEnumerable<object>>(), typeof(TestReactor)).Returns(call =>
        {
            _executed = call.Arg<IEnumerable<object>>().ToArray();
            _order.Add("commands");
            return Result.Success<ReactorSideEffectFailure>();
        });
    }

    public record TestEvent(string Name);

    [Command]
    public record TestCommand(string Name)
    {
        public Task Handle() => Task.CompletedTask;
    }

    [Command]
    public record AmbiguousCommand
    {
        public Task Handle() => Task.CompletedTask;
    }

    public class TestReactor;
}
