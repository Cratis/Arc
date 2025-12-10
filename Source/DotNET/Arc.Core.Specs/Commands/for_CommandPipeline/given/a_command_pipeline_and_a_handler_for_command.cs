// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.given;

public class a_command_pipeline_and_a_handler_for_command : a_command_pipeline
{
    protected ICommandHandler _commandHandler;
    protected string _command;

    void Establish()
    {
        _command = Guid.NewGuid().ToString();
        _commandHandler = Substitute.For<ICommandHandler>();
        var anyHandler = Arg.Any<ICommandHandler>();
        _commandHandlerProviders
            .TryGetHandlerFor(_command, out anyHandler)
            .Returns(r =>
            {
                r[1] = _commandHandler;
                return true;
            });
    }
}
