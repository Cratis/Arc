// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_with_generic_result;

/// <summary>
/// Asking for a base type or interface the response is assignable to must succeed. The pipeline builds the result
/// as CommandResult&lt;concreteType&gt;, which is not CommandResult&lt;TResult&gt; under generic invariance, so a raw cast
/// used to throw InvalidCastException for a legitimately assignable response.
/// </summary>
public class and_handler_returns_subtype_of_requested_type : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult<IAnimal> _result;
    Exception _thrownException;
    Dog _dog;

    void Establish()
    {
        _dog = new Dog("Rex");
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_dog);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _dog).Returns(false);
    }

    async Task Because()
    {
        try
        {
            _result = await _commandPipeline.Execute<IAnimal>(_command, _serviceProvider);
        }
        catch (Exception ex)
        {
            _thrownException = ex;
        }
    }

    [Fact] void should_not_throw() => _thrownException.ShouldBeNull();
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_the_response_as_the_requested_type() => _result.Response.ShouldEqual(_dog);

    interface IAnimal;

    record Dog(string Name) : IAnimal;
}
