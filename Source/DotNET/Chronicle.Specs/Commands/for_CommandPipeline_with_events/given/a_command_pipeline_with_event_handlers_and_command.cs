// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandPipeline_with_events.given;

public class a_command_pipeline_with_event_handlers_and_command : a_command_pipeline_with_event_handlers
{
    protected ICommandHandler _commandHandler;
    protected TestCommand _command;

    void Establish()
    {
        _command = new TestCommand { EventSourceId = EventSourceId.New() };
        _commandHandler = Substitute.For<ICommandHandler>();
        var anyHandler = Arg.Any<ICommandHandler>();
        _commandHandlerProviders
            .TryGetHandlerFor(_command, out anyHandler)
            .Returns(r =>
            {
                r[1] = _commandHandler;
                return true;
            });

        _commandContextValuesBuilder.Build(_command).Returns(new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSourceId, _command.EventSourceId }
        });

        // Mark test events as valid event types
        _eventTypes.HasFor(typeof(TestEvent)).Returns(true);
        _eventTypes.HasFor(typeof(AnotherTestEvent)).Returns(true);
    }

    protected class TestCommand
    {
        public EventSourceId EventSourceId { get; set; } = EventSourceId.Unspecified;
    }
}
