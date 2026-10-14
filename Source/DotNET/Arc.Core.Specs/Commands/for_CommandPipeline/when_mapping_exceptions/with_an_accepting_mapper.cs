// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_mapping_exceptions;

public class with_an_accepting_mapper : given.a_throwing_command_with_exception_mapping
{
    void Establish() => _mapper.TryMap(Arg.Any<CommandContext>(), _exception, out Arg.Any<CommandResult>()).Returns(call =>
    {
        call[2] = _mapped;
        return true;
    });
    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_return_the_mapped_rejection() => _result.ValidationResults.ShouldContainOnly(_mapped.ValidationResults.Single());
    [Fact] void should_not_expose_the_original_message() => _result.ExceptionMessages.ShouldBeEmpty();
    [Fact] void should_preserve_the_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_supply_the_invocation_and_typed_exception() => _mapper.Received(1).TryMap(Arg.Is<CommandContext>(context => context.Type == _command.GetType() && context.CorrelationId == _correlationId && ReferenceEquals(context.ServiceProvider, _serviceProvider)), _exception, out Arg.Any<CommandResult>());
}
