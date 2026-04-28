// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandResponseValueHandlers.when_handling;

public class and_one_of_two_handlers_can_handle : Specification
{
    CommandResponseValueHandlers _handlers;

    ICommandResponseValueHandler _firstHandler;
    ICommandResponseValueHandler _secondHandler;
    CommandContext _context;
    string _value;

    void Establish()
    {
        _firstHandler = Substitute.For<ICommandResponseValueHandler>();
        _firstHandler.CanHandle(Arg.Any<CommandContext>(), Arg.Any<object>()).Returns(false);
        _secondHandler = Substitute.For<ICommandResponseValueHandler>();
        _secondHandler.CanHandle(Arg.Any<CommandContext>(), Arg.Any<object>()).Returns(true);
        _secondHandler.Handle(Arg.Any<CommandContext>(), Arg.Any<object>()).Returns(CommandResult.Success(CorrelationId.New()));

        _handlers = new(new KnownInstancesOf<ICommandResponseValueHandler>([_firstHandler, _secondHandler]));

        _context = new(CorrelationId.New(), typeof(string), "Something", [], new());
        _value = "Forty two";
    }

    async Task Because() => await _handlers.Handle(_context, _value);

    [Fact] void should_not_call_handle_on_handler_that_can_not_handle() => _firstHandler.DidNotReceive().Handle(_context, _value);
    [Fact] void should_call_handle_on_handler_that_can_handle() => _secondHandler.Received().Handle(_context, _value);
}