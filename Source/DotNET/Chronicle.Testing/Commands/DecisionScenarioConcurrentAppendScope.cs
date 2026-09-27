// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>Inserts queued competing events after the handler's decision reads and before Chronicle's owner commit.</summary>
public sealed class DecisionScenarioConcurrentAppendScope : ICommandOperationExecutionScope
{
    readonly DecisionCommandScenario? _scenario;

    /// <summary>Constructs a no-op scope for ordinary scenarios discovered by type discovery.</summary>
    public DecisionScenarioConcurrentAppendScope()
    {
    }

    /// <summary>Constructs the scope used by a decision scenario.</summary>
    /// <param name="scenario">The opted-in scenario.</param>
    internal DecisionScenarioConcurrentAppendScope(DecisionCommandScenario scenario) => _scenario = scenario;

    /// <inheritdoc/>
    public bool IsCommitParticipant => false;

    /// <inheritdoc/>
    public CommandCommitDisposition GetCommitDisposition(CommandContext context) => CommandCommitDisposition.NoCommit;

    /// <inheritdoc/>
    public void Begin(CommandContext context)
    {
    }

    /// <inheritdoc/>
    public Task Complete(CommandContext context, CommandResult result) =>
        _scenario is not null && result.IsSuccess ? _scenario.AppendCompetingEvents() : Task.CompletedTask;
}
