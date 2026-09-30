// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Separates a command's appends from setup and competing appends around the owner commit.
/// </summary>
public sealed class DecisionScenarioCommandCaptureScope : ICommandOperationExecutionScope
{
    readonly DecisionCommandScenario? _scenario;

    /// <summary>
    /// Constructs a no-op scope for ordinary scenarios discovered by type discovery.
    /// </summary>
    public DecisionScenarioCommandCaptureScope()
    {
    }

    /// <summary>
    /// Constructs the scope used by a decision scenario.
    /// </summary>
    /// <param name="scenario">The opted-in scenario.</param>
    internal DecisionScenarioCommandCaptureScope(DecisionCommandScenario scenario) => _scenario = scenario;

    /// <inheritdoc/>
    public bool IsCommitParticipant => false;

    /// <inheritdoc/>
    public CommandCommitDisposition GetCommitDisposition(CommandContext context) => CommandCommitDisposition.NoCommit;

    /// <inheritdoc/>
    public void Begin(CommandContext context) => _scenario?.Begin();

    /// <inheritdoc/>
    public Task Complete(CommandContext context, CommandResult result)
    {
        _scenario?.End();
        return Task.CompletedTask;
    }
}
