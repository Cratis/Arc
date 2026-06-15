// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

/// <summary>
/// The HTTP endpoint and any direct caller of <see cref="ICommandPipeline"/> invoke
/// <c>Execute(command, serviceProvider)</c> with a request/command-scoped provider. This proves that provider
/// is carried on the <see cref="CommandContext"/> so validators resolve from the same scope as the handler.
/// </summary>
public class and_an_explicit_service_provider_is_supplied : given.a_command_pipeline_and_a_handler_for_command
{
    CommandContext _capturedContext = null!;
    IServiceProvider _explicitServiceProvider = null!;

    void Establish()
    {
        _explicitServiceProvider = Substitute.For<IServiceProvider>();
        _commandFilters.OnExecution(Arg.Do<CommandContext>(context => _capturedContext = context))
            .Returns(CommandResult.Success(_correlationId));
    }

    async Task Because() => await _commandPipeline.Execute(_command, _explicitServiceProvider);

    [Fact] void should_carry_the_explicit_service_provider_on_the_context() => _capturedContext.ServiceProvider.ShouldEqual(_explicitServiceProvider);
}
