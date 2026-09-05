// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

/// <summary>
/// Handles <see cref="DependencyHandledValue"/> on the server.
/// </summary>
public class DependencyHandledValueHandler :
    ICommandResponseValueHandler,
    ICommandResponseValueHandler<DependencyHandledValue>,
    ICommandResponseValueHandler<IEnumerable<DependencyHandledValue>>
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value) =>
        value is DependencyHandledValue or IEnumerable<DependencyHandledValue>;

    /// <inheritdoc/>
    public Task<CommandResult> Handle(CommandContext commandContext, object value) =>
        Task.FromResult(CommandResult.Success(commandContext.CorrelationId));
}
