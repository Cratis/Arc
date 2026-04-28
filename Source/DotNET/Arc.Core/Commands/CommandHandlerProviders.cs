// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.DependencyInjection;
using Cratis.Types;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents an implementation of <see cref="ICommandHandlerProviders"/>.
/// </summary>
[Singleton]
public class CommandHandlerProviders : ICommandHandlerProviders
{
    readonly IDictionary<Type, ICommandHandler> _providers;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandHandlerProviders"/> class.
    /// </summary>
    /// <param name="providers">The collection of <see cref="ICommandHandlerProvider"/> to use for providing command handlers.</param>
    public CommandHandlerProviders(IInstancesOf<ICommandHandlerProvider> providers)
    {
        var handlers = providers.SelectMany(p => p.Handlers);
        MultipleCommandHandlersForSameCommandType.ThrowIfDuplicates(handlers);
        _providers = handlers.ToDictionary(h => h.CommandType, h => h);
    }

    /// <inheritdoc/>
    public IEnumerable<ICommandHandler> Handlers => _providers.Values;

    /// <inheritdoc/>
    public bool TryGetHandlerFor(object command, [NotNullWhen(true)] out ICommandHandler? handler) =>
        _providers.TryGetValue(command.GetType(), out handler);
}
