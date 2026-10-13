// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing_streams.given;

public class a_pending_completion : for_TransactionalCommandScope.given.a_transactional_command_scope
{
    protected IEventLog _eventLog;
    protected CompleteStreamCommandResponseValueHandler _handler;
    protected CommandResult _result;
    protected List<string> _operations;

    void Establish()
    {
        _eventLog = Substitute.For<IEventLog>();
        _eventLog.Id.Returns(EventSequenceId.Log);
        _operations = [];
        _eventLog.CompleteStream("completion", "period").Returns(_ =>
        {
            _operations.Add("complete");
            Result<EventSequenceNumber, CompleteStreamError> completed = EventSequenceNumber.First;
            return Task.FromResult(completed);
        });
        _unitOfWork.Commit().Returns(_ =>
        {
            _operations.Add("commit");
            return Task.CompletedTask;
        });
        _handler = new(_eventLog);
        _result = CommandResult.Success(_correlationId);
    }

    protected void ResolveSource(object command)
    {
        _context = _context with { Type = command.GetType(), Command = command };
        var provider = new EventSourceValuesProvider(NullLogger<EventSourceValuesProvider>.Instance);
        foreach (var (key, value) in provider.Provide(command))
        {
            _context.Values[key] = value;
        }
    }

    protected Task<CommandResult> Enroll()
    {
        _scope.Begin(_context);

        return _handler.Handle(_context, new CompleteStream("completion", "period"));
    }
}
