// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Cratis.Execution;

namespace Cratis.Arc.Commands;

/// <summary>
/// Wraps command responses in <see cref="CommandResult{TResponse}"/> for their runtime type, using the typed factories
/// the Arc source generator emits for the response types each command's Handle method declares.
/// </summary>
/// <remarks>
/// Not intended to be called directly. A response whose runtime type has no generated factory, such as a subtype of the
/// declared response type or a command without generated code, is wrapped through reflection where dynamic code is
/// supported. This registry does not imply that the rest of Arc supports NativeAOT.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CommandResultFactories
{
    static readonly ConcurrentDictionary<Type, Func<CorrelationId, object, CommandResult>> _factories = new();

    /// <summary>
    /// Registers a typed factory emitted by the Arc source generator.
    /// </summary>
    /// <param name="responseType">The exact runtime type of the response the factory wraps.</param>
    /// <param name="factory">Creates a <see cref="CommandResult{TResponse}"/> of <paramref name="responseType"/> holding the response.</param>
    public static void Register(Type responseType, Func<CorrelationId, object, CommandResult> factory) => _factories[responseType] = factory;

    /// <summary>
    /// Wraps a response in a <see cref="CommandResult{TResponse}"/> of its runtime type using a generated factory.
    /// </summary>
    /// <param name="correlationId">The <see cref="CorrelationId"/> of the command.</param>
    /// <param name="response">The response to wrap.</param>
    /// <param name="result">The <see cref="CommandResult{TResponse}"/> holding the response, when a factory is registered.</param>
    /// <returns>True when a factory is registered for the runtime type of the response; otherwise false.</returns>
    internal static bool TryCreate(CorrelationId correlationId, object response, [NotNullWhen(true)] out CommandResult? result)
    {
        result = _factories.TryGetValue(response.GetType(), out var factory) ? factory(correlationId, response) : null;
        return result is not null;
    }

    /// <summary>
    /// Gets the factory registered for an exact response type.
    /// </summary>
    /// <param name="responseType">The response type.</param>
    /// <returns>The registered factory, or null when the type has none.</returns>
    internal static Func<CorrelationId, object, CommandResult>? For(Type responseType) =>
        _factories.TryGetValue(responseType, out var factory) ? factory : null;

    /// <summary>
    /// Wraps a response in a <see cref="CommandResult{TResponse}"/> of its runtime type through reflection.
    /// </summary>
    /// <param name="correlationId">The <see cref="CorrelationId"/> of the command.</param>
    /// <param name="response">The response to wrap.</param>
    /// <returns>The <see cref="CommandResult{TResponse}"/> holding the response.</returns>
    [RequiresDynamicCode("Constructs CommandResult<T> for a response type only known at runtime.")]
    internal static CommandResult CreateThroughReflection(CorrelationId correlationId, object response)
    {
        var commandResultType = typeof(CommandResult<>).MakeGenericType(response.GetType());
        return (Activator.CreateInstance(commandResultType, correlationId, response) as CommandResult)!;
    }
}
