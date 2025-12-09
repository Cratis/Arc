// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandResponseValueHandlers.when_handling;

public class and_two_handlers_can_handle : Specification
{
    CommandResponseValueHandlers _handlers;

    ICommandResponseValueHandler _firstHandler;
    ICommandResponseValueHandler _secondHandler;
    CommandContext _context;
    string _value;

    CommandResult _firstCommandResult;
    CommandResult _secondCommandResult;
    CommandResult _result;

    void Establish()
    {
        _firstCommandResult = new()
        {
            IsAuthorized = false
        };
        _firstHandler = Substitute.For<ICommandResponseValueHandler>();
        _firstHandler.CanHandle(Arg.Any<CommandContext>(), Arg.Any<object>()).Returns(true);
        _firstHandler.Handle(Arg.Any<CommandContext>(), Arg.Any<object>()).Returns(_firstCommandResult);

        _secondCommandResult = new()
        {
            ValidationResults = [new ValidationResult(ValidationResultSeverity.Warning, "Something is wrong", [], null!)]
        };
        _secondHandler = Substitute.For<ICommandResponseValueHandler>();
        _secondHandler.CanHandle(Arg.Any<CommandContext>(), Arg.Any<object>()).Returns(true);
        _secondHandler.Handle(Arg.Any<CommandContext>(), Arg.Any<object>()).Returns(_secondCommandResult);

        _handlers = new(new KnownInstancesOf<ICommandResponseValueHandler>([_firstHandler, _secondHandler]));

        _context = new(CorrelationId.New(), typeof(string), "Something", [], new());
        _value = "Forty two";
    }

    async Task Because() => _result = await _handlers.Handle(_context, _value);

    [Fact] void should_call_handle_on_first_handler() => _firstHandler.Received().Handle(_context, _value);
    [Fact] void should_call_handle_on_second_handler() => _secondHandler.Received().Handle(_context, _value);
    [Fact]
    void should_return_a_merged_command_result()
    {
        _result.IsAuthorized.ShouldBeFalse();
        _result.IsValid.ShouldBeFalse();
        _result.ValidationResults.ShouldContain(_secondCommandResult.ValidationResults.First());
    }
}