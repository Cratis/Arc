// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalEventLog.when_appending;

public class and_a_command_transaction_is_active : Specification
{
    IEventLog _inner;
    IUnitOfWork _unitOfWork;
    TransactionalEventLog _eventLog;

    void Establish()
    {
        _inner = Substitute.For<IEventLog>();
        _inner.Id.Returns(EventSequenceId.Log);
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _eventLog = new TransactionalEventLog(_inner, Substitute.For<IUnitOfWorkManager>());
    }

    async Task Because()
    {
        CommandTransaction.Current = _unitOfWork;
        await _eventLog.Append(EventSourceId.New(), new object());
        CommandTransaction.Current = null;
    }

    [Fact] void should_enroll_the_event_in_the_unit_of_work() => _unitOfWork.ReceivedWithAnyArgs(1).AddEvent(default!, default!, default!, default!);
    [Fact] void should_not_append_to_the_inner_log() => _inner.DidNotReceiveWithAnyArgs().Append(default!, default!);
}
