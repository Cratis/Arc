// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Execution;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.given;

public class a_refused_invocation : a_command_side_effect_executor
{
    protected TestCommand _command = new("refused");
    protected TestCommand _later = new("later");
    protected CommandResult _commandResult;
    protected CommandInvocation _invocation;
    protected IEventStore _store;
    protected IEventLog _eventLog;
    protected ReactorContext _context;
    protected Result<ReactorSideEffectFailure> _result;
    protected EventForEventSourceId[] _appended = [];
    protected CommandRefusal? _received;
    protected int _branchesRun;

    void Establish()
    {
        _commandResult = new() { ValidationResults = [ValidationResult.Error("$strings.refused")] };
        _commandPipeline.Execute(_command, _serviceProvider).Returns(_ => _commandResult);
        _commandPipeline.Execute(_later, _serviceProvider).Returns(CommandResult.Success(CorrelationId.NotSet));
        _store = Substitute.For<IEventStore>();
        _eventLog = Substitute.For<IEventLog>();
        _store.EventLog.Returns(_eventLog);
        _store.EventTypes.HasFor(typeof(RefusalRecorded)).Returns(true);
        _context = new(EventContext.Empty with { EventSourceId = "observed" }, new TestReactor(), new()) { EventStore = _store };
        _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>()).Returns(call =>
        {
            _appended = call.Arg<IEnumerable<EventForEventSourceId>>().ToArray();
            return AppendManyResult.Success(CorrelationId.NotSet, [EventSequenceNumber.First]);
        });
        _invocation = Invoke.Command(_command).OnRefused(Refused.ByValidation, Record);
    }

    protected object Record(CommandRefusal refusal)
    {
        _received = refusal;
        _branchesRun++;

        return new RefusalRecorded(refusal.Message);
    }

    public record RefusalRecorded(string Message);
}
