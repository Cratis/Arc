// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_handler_returns_tuple_with_value_that_can_be_handled_only_after_response_set : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;
    (string, int) _tuple;

    void Establish()
    {
        _tuple = ("response_value", 42);
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_tuple);

        // The string is not possible to handle (becomes the response)
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _tuple.Item1).Returns(false);

        // The int is possible to handle ONLY when Response is set
        // This simulates the Chronicle scenario where EventsCommandResponseValueHandler
        // checks commandContext.HasEventSourceId() which depends on Response being set
        _commandResponseValueHandlers.CanHandle(
            Arg.Is<CommandContext>(ctx => ctx.Response == null),
            _tuple.Item2).Returns(false);

        _commandResponseValueHandlers.CanHandle(
            Arg.Is<CommandContext>(ctx => ctx.Response != null),
            _tuple.Item2).Returns(true);

        _commandResponseValueHandlers.Handle(Arg.Any<CommandContext>(), _tuple.Item2).Returns(CommandResult.Success(CorrelationId.New()));
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();

    [Fact]
    void should_call_value_handler_for_value_that_can_be_handled() =>
        _commandResponseValueHandlers.Received(1).Handle(Arg.Is<CommandContext>(ctx => ctx.Response!.Equals(_tuple.Item1)), _tuple.Item2);

    [Fact]
    void should_not_call_value_handler_for_unhandled_value() =>
        _commandResponseValueHandlers.DidNotReceive().Handle(Arg.Any<CommandContext>(), _tuple.Item1);

    [Fact]
    void should_set_unhandled_value_as_response_in_context_when_checking_if_it_can_be_handled() =>
        _commandResponseValueHandlers.Received().CanHandle(Arg.Is<CommandContext>(ctx => ctx.Response!.Equals(_tuple.Item1)), _tuple.Item2);
}
