// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Types;

namespace Cratis.Arc.Commands.ModelBound;

/// <summary>
/// Represents a provider for command handlers that are model bound.
/// </summary>
public class CommandHandlerProvider : ICommandHandlerProvider
{
    static readonly ConditionalWeakTable<ITypes, CommandDiscovery> _commandsByUniverse = new();

    readonly Dictionary<Type, MethodInfo> _commandTypes;
    readonly Dictionary<Type, ICommandHandler> _handlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandHandlerProvider"/> class.
    /// </summary>
    /// <param name="types">The types available in the application.</param>
    public CommandHandlerProvider(ITypes types)
    {
        _commandTypes = _commandsByUniverse.GetValue(types, _ => new CommandDiscovery()).GetCommands(types);
        _handlers = _commandTypes.ToDictionary(kv => kv.Key, kv => (ICommandHandler)new ModelBoundCommandHandler(kv.Key, kv.Value));
    }

    /// <inheritdoc/>
    public IEnumerable<ICommandHandler> Handlers => _handlers.Values;

    /// <inheritdoc/>
    public bool TryGetHandlerFor(object command, [NotNullWhen(true)] out ICommandHandler? handler)
    {
        var commandType = command.GetType();
        if (!_commandTypes.ContainsKey(commandType))
        {
            handler = null;
            return false;
        }

        handler = _handlers[commandType];
        return true;
    }

    sealed class CommandDiscovery
    {
        readonly object _gate = new();
        Type[] _types = [];
        Dictionary<Type, MethodInfo> _commands = [];
        bool _initialized;

        public Dictionary<Type, MethodInfo> GetCommands(ITypes universe)
        {
            var types = universe.All.ToArray();
            lock (_gate)
            {
                if (!_initialized || !types.SequenceEqual(_types))
                {
                    _commands = types.Where(t => t.IsCommand()).ToDictionary(t => t, t => t.GetHandleMethod());
                    _types = types;
                    _initialized = true;
                }

                return _commands;
            }
        }
    }
}
