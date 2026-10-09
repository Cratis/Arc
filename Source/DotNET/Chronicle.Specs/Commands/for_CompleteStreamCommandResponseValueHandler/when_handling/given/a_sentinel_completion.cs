// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSequences;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CompleteStreamCommandResponseValueHandler.when_handling.given;

public abstract class a_sentinel_completion : Specification
{
    protected CompleteStreamCommandResponseValueHandler _handler;
    protected CommandContext _context;
    protected IEventLog _eventLog;
    protected CommandResult _result;

    void Establish()
    {
        _eventLog = Substitute.For<IEventLog>();
        _handler = new(_eventLog);
        _context = new(CorrelationId.New(), typeof(object), new object(), [], new());
    }
}
