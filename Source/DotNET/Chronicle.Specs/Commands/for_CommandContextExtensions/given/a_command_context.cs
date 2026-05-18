// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandContextExtensions.given;

public class a_command_context : Specification
{
    protected CommandContext _commandContext;
    protected CorrelationId _correlationId;
    protected TestCommand _command;
    protected CommandContextValues _commandContextValues;

    void Establish()
    {
        _correlationId = Guid.NewGuid();
        _command = new TestCommand();
        _commandContextValues = [];
        _commandContext = new CommandContext(_correlationId, typeof(TestCommand), _command, [], _commandContextValues, null);
    }

    public class TestCommand;
}
