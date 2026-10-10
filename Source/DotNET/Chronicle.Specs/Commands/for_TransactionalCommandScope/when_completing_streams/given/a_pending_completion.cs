// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

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

    protected Task<CommandResult> Enroll()
    {
        _scope.Begin(_context);

        return _handler.Handle(_context, new CompleteStream("completion", "period"));
    }
}
