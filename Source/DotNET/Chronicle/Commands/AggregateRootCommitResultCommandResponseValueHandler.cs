// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents a command response value handler that converts an <see cref="AggregateRootCommitResult"/>
/// into a <see cref="CommandResult"/>, propagating validation results, constraint violations,
/// concurrency violations, and errors back to the command pipeline.
/// </summary>
public class AggregateRootCommitResultCommandResponseValueHandler : ICommandResponseValueHandler
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value) =>
        value is AggregateRootCommitResult;

    /// <inheritdoc/>
    public Task<CommandResult> Handle(CommandContext commandContext, object value)
    {
        var commitResult = (AggregateRootCommitResult)value;
        return Task.FromResult(commitResult.ToCommandResult(commandContext.CorrelationId));
    }
}
