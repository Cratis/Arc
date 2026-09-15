// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Tracks flat command participation without retaining service scopes beyond command completion.
/// </summary>
/// <param name="parent">The enclosing command, if any.</param>
/// <param name="host">The originating Arc host identity.</param>
/// <param name="mayParticipate">Whether the declared result can contain operations.</param>
internal sealed class CommandOperationFrame(CommandOperationFrame? parent, object host, bool mayParticipate) : IDisposable
{
    int _nestedCommandAttempts;

    /// <summary>
    /// Gets the enclosing command frame.
    /// </summary>
    public CommandOperationFrame? Parent { get; } = parent;

    /// <summary>
    /// Gets the host identity separating independently hosted in-process transports from nested application commands.
    /// </summary>
    public object Host { get; } = host;

    /// <summary>
    /// Gets or sets whether operations may participate.
    /// </summary>
    public bool MayParticipate { get; set; } = mayParticipate;

    /// <summary>
    /// Gets whether nested execution was attempted.
    /// </summary>
    public bool HasNestedCommand => NestedCommandAttempts != 0;

    /// <summary>
    /// Gets the number of same-host nested command attempts, including rejected attempts.
    /// </summary>
    public int NestedCommandAttempts => Volatile.Read(ref _nestedCommandAttempts);

    /// <summary>
    /// Records a rejected or legacy nested command attempt on its owning frame.
    /// </summary>
    public void RecordNestedCommand() => Interlocked.Increment(ref _nestedCommandAttempts);

    /// <summary>
    /// Rejects nested work attempted during an operation callback, even if its failed result was ignored.
    /// </summary>
    /// <param name="previousAttempts">The number of attempts observed before the callback.</param>
    /// <exception cref="InvalidCommandOperation">The callback attempted unsupported nesting.</exception>
    public void ValidateNoNewNestedCommands(int previousAttempts)
    {
        if (NestedCommandAttempts != previousAttempts)
        {
            throw new InvalidCommandOperation("Nested commands are not supported during command operation execution or compensation. The child has not executed.");
        }
    }

    /// <inheritdoc/>
    public void Dispose() => CommandOperationBoundary.Leave(this);

    /// <summary>
    /// Rejects unsupported boundaries before entering operations.
    /// </summary>
    /// <param name="scopes">The materialized command scopes.</param>
    /// <exception cref="InvalidCommandOperation">Nesting or scope compatibility is unsupported.</exception>
    public void Validate(ICommandExecutionScope[] scopes)
    {
        if ((Parent is not null && ReferenceEquals(Parent.Host, Host)) || HasNestedCommand)
        {
            throw new InvalidCommandOperation("Command operations require a flat command boundary; nested execution was rejected before operations started.");
        }

        if (scopes.Any(scope => scope is not ICommandOperationExecutionScope) ||
            scopes.OfType<ICommandOperationExecutionScope>().Count(scope => scope.IsCommitParticipant) > 1)
        {
            throw new InvalidCommandOperation("Command operations require explicitly compatible ICommandOperationExecutionScope scopes and at most one deferred commit participant.");
        }
    }
}
