// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Immutable server-only original failure context optionally injected into Compensate. It cannot change command success.
/// </summary>
public sealed class CommandOperationFailure
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandOperationFailure"/> class from frozen pipeline observations.
    /// </summary>
    /// <param name="invocationIndex">The invocation index.</param>
    /// <param name="invocationCompleted">Whether execution returned.</param>
    /// <param name="isFailingInvocation">Whether this invocation threw.</param>
    /// <param name="source">The original failure phase.</param>
    /// <param name="commitDisposition">The observed commit facts.</param>
    /// <param name="exceptionMessages">Original exception messages to snapshot.</param>
    internal CommandOperationFailure(
        int invocationIndex,
        bool invocationCompleted,
        bool isFailingInvocation,
        CommandOperationFailureSource source,
        CommandCommitDisposition commitDisposition,
        IEnumerable<string> exceptionMessages)
    {
        InvocationIndex = invocationIndex;
        InvocationCompleted = invocationCompleted;
        IsFailingInvocation = isFailingInvocation;
        Source = source;
        CommitDisposition = commitDisposition;
        ExceptionMessages = Array.AsReadOnly(exceptionMessages.ToArray());
    }

    /// <summary>
    /// Gets the zero-based invocation index.
    /// </summary>
    public int InvocationIndex { get; }

    /// <summary>
    /// Gets whether this invocation's Execute returned.
    /// </summary>
    public bool InvocationCompleted { get; }

    /// <summary>
    /// Gets whether this invocation threw the original failure.
    /// </summary>
    public bool IsFailingInvocation { get; }

    /// <summary>
    /// Gets the original failure phase.
    /// </summary>
    public CommandOperationFailureSource Source { get; }

    /// <summary>
    /// Gets coordinated commitment facts used to authorize recovery.
    /// </summary>
    public CommandCommitDisposition CommitDisposition { get; }

    /// <summary>
    /// Gets a defensive snapshot of original exception messages. Recovery failures are reported separately.
    /// </summary>
    public IReadOnlyList<string> ExceptionMessages { get; }
}
