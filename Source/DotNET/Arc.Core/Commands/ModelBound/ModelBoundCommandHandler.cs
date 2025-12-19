// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Tasks;

namespace Cratis.Arc.Commands.ModelBound;

/// <summary>
/// Represents an implementation of <see cref="ICommandHandler"/> for model-bound commands.
/// </summary>
/// <param name="commandType">The type of command the handler can handle.</param>
/// <param name="handleMethod">The method info of the handle method.</param>
public class ModelBoundCommandHandler(Type commandType, MethodInfo handleMethod) : ICommandHandler
{
    /// <inheritdoc/>
    public IEnumerable<string> Location { get; } = commandType.Namespace?.Split('.') ?? [];

    /// <inheritdoc/>
    public Type CommandType => commandType;

    /// <inheritdoc/>
    public IEnumerable<Type> Dependencies { get; } = handleMethod.GetParameters().Select(p => p.ParameterType);

    /// <inheritdoc/>
    public bool AllowsAnonymousAccess { get; } = commandType.IsAnonymousAllowed();

    /// <inheritdoc/>
    public async ValueTask<object?> Handle(CommandContext commandContext)
    {
        var parameters = handleMethod.GetParameters();
        var args = parameters.Length == 0
            ? null
            : commandContext.Dependencies.Take(parameters.Length).ToArray();
        var invocationResult = handleMethod.Invoke(commandContext.Command, args);

        var (_, result) = await AwaitableHelpers.AwaitIfNeeded(invocationResult);
        return result;
    }
}
