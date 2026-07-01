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
/// Represents a reactor side effect handler for multiple commands.
/// </summary>
/// <param name="serviceScopeFactory">The <see cref="IServiceScopeFactory"/> to create a scope for command execution.</param>
[Singleton]
public class CommandsResultHandler(IServiceScopeFactory serviceScopeFactory) : IReactorSideEffectHandler
{
    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, object value) =>
        value is IEnumerable<object> objects && objects.Any() && objects.All(o => o is not null && o.GetType().IsCommand());

    /// <inheritdoc/>
    public async Task Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
    {
        var commands = (IEnumerable<object>)value;
        if (!commands.Any())
        {
            return;
        }

        // Create a new service scope for these command executions to ensure transactional behavior
        using var scope = serviceScopeFactory.CreateScope();
        var commandPipeline = scope.ServiceProvider.GetRequiredService<ICommandPipeline>();

        // Execute each command within the same scope
        foreach (var command in commands)
        {
            var result = await commandPipeline.Execute(command, scope.ServiceProvider);

            // If any command execution fails, throw an exception to fail the reactor
            if (!result.IsSuccess)
            {
                throw new CommandExecutionFailedException(command.GetType(), result);
            }
        }
    }
}
