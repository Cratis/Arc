// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Reactors;

/// <summary>
/// A returned command with ordered, invocation-local refusal branches.
/// </summary>
public sealed class CommandInvocation
{
    readonly RefusalBranch[] _branches;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandInvocation"/> class.
    /// </summary>
    /// <param name="command">The protected command.</param>
    /// <param name="branches">The ordered branches.</param>
    internal CommandInvocation(object command, RefusalBranch[] branches)
    {
        Command = command;
        _branches = branches;
    }

    /// <summary>
    /// Gets the command to execute through Arc's protected pipeline.
    /// </summary>
    public object Command { get; }

    /// <summary>
    /// Adds a branch. The first matching branch handles the refusal and stops remaining invocations.
    /// </summary>
    /// <param name="selector">The refusal selector.</param>
    /// <param name="handler">Produces a registered event, a stable event sequence, or null to acknowledge.</param>
    /// <returns>A new invocation retaining the existing branches in declaration order.</returns>
    public CommandInvocation OnRefused(Refused selector, Func<CommandRefusal, object?> handler) =>
        new(Command, [.. _branches, new(selector, handler)]);

    /// <summary>
    /// Adds a branch that acknowledges a matching refusal without producing events.
    /// </summary>
    /// <param name="selector">The refusal selector.</param>
    /// <returns>A new invocation with an acknowledgment branch.</returns>
    public CommandInvocation Acknowledge(Refused selector) => OnRefused(selector, Invoke.Acknowledge);

    /// <summary>
    /// Selects the first matching branch in declaration order.
    /// </summary>
    /// <param name="refusal">The classified refusal.</param>
    /// <returns>The matching branch, or null.</returns>
    internal RefusalBranch? FindBranch(CommandRefusal refusal) =>
        _branches.FirstOrDefault(branch => branch.Selector.Matches(refusal));
}
