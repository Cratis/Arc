// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Reactors;

/// <summary>
/// Represents a reactor side effect handler for a single command.
/// </summary>
/// <param name="serviceScopeFactory">The <see cref="IServiceScopeFactory"/> to create a scope for command execution.</param>
[Singleton]
public class CommandResultHandler(IServiceScopeFactory serviceScopeFactory) : IReactorSideEffectHandler
{
    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, object value) =>
        value?.GetType().IsCommand() == true;

    /// <inheritdoc/>
    public async Task Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
    {
        // Create a new service scope for this command execution to ensure transactional behavior
        using var scope = serviceScopeFactory.CreateScope();
        var commandPipeline = scope.ServiceProvider.GetRequiredService<ICommandPipeline>();

        // Execute the command within the scope
        var result = await commandPipeline.Execute(value, scope.ServiceProvider);

        // If the command execution failed, throw an exception to fail the reactor
        if (!result.IsSuccess)
        {
            throw new CommandExecutionFailedException(value.GetType(), result);
        }
    }
}
