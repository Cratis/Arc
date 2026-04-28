// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandResponseValueHandlers.when_checking_can_handle;

public class and_handler_can_handle : Specification
{
    CommandResponseValueHandlers _handlers;
    ICommandResponseValueHandler _handler;
    CommandContext _context;
    string _value;
    bool _result;

    void Establish()
    {
        _handler = Substitute.For<ICommandResponseValueHandler>();
        _handler.CanHandle(Arg.Any<CommandContext>(), Arg.Any<object>()).Returns(true);

        _handlers = new(new KnownInstancesOf<ICommandResponseValueHandler>([_handler]));

        _context = new(CorrelationId.New(), typeof(string), "Something", [], new());
        _value = "Forty two";
    }

    void Because() => _result = _handlers.CanHandle(_context, _value);

    [Fact] void should_return_true() => _result.ShouldBeTrue();
    [Fact] void should_check_handler() => _handler.Received(1).CanHandle(_context, _value);
}