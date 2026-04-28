// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Cratis.Types;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents an implementation of <see cref="ICommandResponseValueHandlers"/>.
/// </summary>
/// <param name="handlers">The available <see cref="ICommandResponseValueHandler"/>.</param>
[Singleton]
public class CommandResponseValueHandlers(IInstancesOf<ICommandResponseValueHandler> handlers) : ICommandResponseValueHandlers
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext context, object value) =>
        handlers.Any(handler => handler.CanHandle(context, value));

    /// <inheritdoc/>
    public async Task<CommandResult> Handle(CommandContext context, object value)
    {
        var handlersThatCanHandle = handlers.Where(handler => handler.CanHandle(context, value)).ToArray();
        var commandResult = CommandResult.Success(context.CorrelationId);
        if (handlersThatCanHandle.Length != 0)
        {
            foreach (var handler in handlersThatCanHandle)
            {
                var result = await handler.Handle(context, value);
                commandResult.MergeWith(result);
            }
        }

        return commandResult;
    }
}