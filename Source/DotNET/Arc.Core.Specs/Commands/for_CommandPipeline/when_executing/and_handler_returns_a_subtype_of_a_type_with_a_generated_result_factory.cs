// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_handler_returns_a_subtype_of_a_type_with_a_generated_result_factory : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;
    DerivedResponse _response;
    bool _baseFactoryInvoked;

    void Establish()
    {
        _response = new(42);
        CommandResultFactories.Register(typeof(BaseResponse), (correlationId, response) =>
        {
            _baseFactoryInvoked = true;
            return new CommandResult<BaseResponse>(correlationId, (BaseResponse)response);
        });
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_response);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _response).Returns(false);
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_have_no_factory_for_the_derived_type() => CommandResultFactories.For(typeof(DerivedResponse)).ShouldBeNull();
    [Fact] void should_return_command_result_of_the_derived_type() => _result.ShouldBeOfExactType<CommandResult<DerivedResponse>>();
    [Fact] void should_have_response_value() => ((CommandResult<DerivedResponse>)_result).Response.ShouldEqual(_response);
    [Fact] void should_not_invoke_the_factory_of_the_base_type() => _baseFactoryInvoked.ShouldBeFalse();

    public record BaseResponse(int Value);

    public record DerivedResponse(int Value) : BaseResponse(Value);
}
