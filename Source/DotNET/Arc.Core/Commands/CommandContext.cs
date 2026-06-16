// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents the context for a command being executed.
/// </summary>
/// <param name="CorrelationId">The correlation ID for the command.</param>
/// <param name="Type">The type of the command.</param>
/// <param name="Command">The command instance.</param>
/// <param name="Dependencies">The dependencies required to handle the command.</param>
/// <param name="Values">A set of values associated with the command context.</param>
/// <param name="AllowedSeverity">The maximum validation result severity level to allow. Validation results with severity higher than this will cause the command to fail.</param>
/// <param name="Response">The optional response from handling the command, if any.</param>
/// <param name="ServiceProvider">The <see cref="IServiceProvider"/> scoped to the command, used to resolve scoped collaborators such as validators and read models during the command's lifetime.</param>
/// <param name="CancellationToken">The cancellation token for the command execution.</param>
public record CommandContext(
    CorrelationId CorrelationId,
    Type Type,
    object Command,
    IEnumerable<object?> Dependencies,
    CommandContextValues Values,
    ValidationResultSeverity? AllowedSeverity = default,
    object? Response = default,
    IServiceProvider? ServiceProvider = default,
    CancellationToken CancellationToken = default)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandContext"/> record.
    /// </summary>
    /// <param name="correlationId">The correlation ID for the command.</param>
    /// <param name="type">The type of the command.</param>
    /// <param name="command">The command instance.</param>
    /// <param name="dependencies">The dependencies required to handle the command.</param>
    /// <param name="values">A set of values associated with the command context.</param>
    /// <param name="allowedSeverity">The maximum validation result severity level to allow. Validation results with severity higher than this will cause the command to fail.</param>
    /// <param name="response">The optional response from handling the command, if any.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> scoped to the command, used to resolve scoped collaborators such as validators and read models during the command's lifetime.</param>
    public CommandContext(
        CorrelationId correlationId,
        Type type,
        object command,
        IEnumerable<object?> dependencies,
        CommandContextValues values,
        ValidationResultSeverity? allowedSeverity,
        object? response,
        IServiceProvider? serviceProvider)
        : this(correlationId, type, command, dependencies, values, allowedSeverity, response, serviceProvider, default)
    {
    }
}
