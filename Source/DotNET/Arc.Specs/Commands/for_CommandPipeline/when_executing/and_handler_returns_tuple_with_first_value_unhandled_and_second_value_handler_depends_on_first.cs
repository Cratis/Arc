// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_handler_returns_tuple_with_first_value_unhandled_and_second_value_handler_depends_on_first : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult<Guid> _result;
    (Guid, int) _tuple;
    CommandContext _capturedContext;

    void Establish()
    {
        _tuple = (Guid.NewGuid(), 42);
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_tuple);

        // The Guid (first value) has no handler - it should become the response
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _tuple.Item1).Returns(false);

        // The int (second value) can only be handled if the Response is set
        // This simulates a scenario where the handler needs to know the identity from the first value
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _tuple.Item2)
            .Returns(callInfo =>
            {
                var ctx = callInfo.ArgAt<CommandContext>(0);
                return ctx.Response is Guid;
            });

        _commandResponseValueHandlers.Handle(Arg.Any<CommandContext>(), _tuple.Item2)
            .Returns(callInfo =>
            {
                _capturedContext = callInfo.ArgAt<CommandContext>(0);
                return Task.FromResult(CommandResult.Success(CorrelationId.New()));
            });
    }

    async Task Because() => _result = (await _commandPipeline.Execute(_command)) as CommandResult<Guid>;

    [Fact] void should_set_first_unhandled_value_as_response() => _result.Response.ShouldEqual(_tuple.Item1);
    [Fact] void should_call_handler_for_second_value() => _commandResponseValueHandlers.Received(1).Handle(Arg.Any<CommandContext>(), _tuple.Item2);
    [Fact] void should_pass_context_with_response_to_handler() => _capturedContext.Response.ShouldEqual(_tuple.Item1);
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
