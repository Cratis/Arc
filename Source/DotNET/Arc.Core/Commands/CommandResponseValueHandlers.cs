// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
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
    public void UpdateContext(CommandContext context, object value)
    {
        foreach (var handler in HandlersFor(context).OfType<ICommandResponseValueContextUpdater>().Where(_ => _.CanHandle(context, value)))
        {
            handler.UpdateContext(context, value);
        }
    }

    /// <inheritdoc/>
    public bool CanHandle(CommandContext context, object value) =>
        HandlersFor(context).Any(handler => handler.CanHandle(context, value));

    /// <inheritdoc/>
    public async Task<CommandResult> Handle(CommandContext context, object value)
    {
        var handlersThatCanHandle = HandlersFor(context).Where(handler => handler.CanHandle(context, value)).ToArray();
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

    /// <summary>
    /// Gets the handlers to use for a command, resolved from the command's own scope rather than the provider that
    /// constructed this singleton, so a handler depending on a scoped service — as every Chronicle handler does
    /// through <c>IEventLog</c> — is created in the scope the command runs in instead of the root.
    /// </summary>
    /// <param name="context">The <see cref="CommandContext"/> for the command being handled.</param>
    /// <returns>The available <see cref="ICommandResponseValueHandler"/>.</returns>
    IEnumerable<ICommandResponseValueHandler> HandlersFor(CommandContext context) =>
        DiscoveredInstances.ResolvedFrom(context.ServiceProvider, handlers);
}
