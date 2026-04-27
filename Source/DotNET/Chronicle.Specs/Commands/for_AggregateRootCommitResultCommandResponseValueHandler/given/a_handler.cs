// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_AggregateRootCommitResultCommandResponseValueHandler.given;

public class a_handler : Specification
{
    protected AggregateRootCommitResultCommandResponseValueHandler _handler;
    protected CommandContext _commandContext;
    protected CorrelationId _correlationId;

    void Establish()
    {
        _handler = new AggregateRootCommitResultCommandResponseValueHandler();
        _correlationId = Guid.NewGuid();
        _commandContext = new CommandContext(
            _correlationId,
            typeof(object),
            new object(),
            [],
            new CommandContextValues(),
            null);
    }
}
