// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalEventLog.when_appending;

public class and_no_command_transaction_is_active : Specification
{
    IEventLog _inner;
    TransactionalEventLog _eventLog;

    void Establish()
    {
        _inner = Substitute.For<IEventLog>();
        _eventLog = new TransactionalEventLog(_inner);
    }

    async Task Because() => await _eventLog.Append(EventSourceId.New(), new object());

    [Fact] void should_append_to_the_inner_log_immediately() => _inner.ReceivedWithAnyArgs(1).Append(default!, default!);
}
