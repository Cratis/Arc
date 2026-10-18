// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;

namespace Cratis.Arc.Chronicle.Occurrences;

/// <summary>
/// Defines a system that executes a command for a Chronicle occurrence, keyed by the occurrence and an effect name,
/// so that a redelivered occurrence never runs a successful command twice.
/// </summary>
/// <remarks>
/// The effect receipt is written atomically with the command's events. A business refusal is terminal and recorded;
/// concurrency conflicts, infrastructure failures, handler exceptions and authorization failures leave no receipt and
/// can be retried. Requires the Chronicle kernel's effect receipt capability and fails closed with
/// <see cref="EffectReceiptsNotSupported"/> without it.
/// </remarks>
public interface IOccurrenceCommandExecutor
{
    /// <summary>
    /// Executes a command for an occurrence effect, running as the system with no roles.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="occurrence">The <see cref="OccurrenceContext"/> the effect belongs to.</param>
    /// <param name="effectName">The stable name of the effect within the occurrence.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="OccurrenceCommandResult"/>.</returns>
    /// <exception cref="EffectReceiptsNotSupported">The kernel capability is absent or unknown.</exception>
    Task<OccurrenceCommandResult> Execute(object command, OccurrenceContext occurrence, EffectName effectName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command for an occurrence effect, running as the system with the explicitly given roles.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="occurrence">The <see cref="OccurrenceContext"/> the effect belongs to.</param>
    /// <param name="effectName">The stable name of the effect within the occurrence.</param>
    /// <param name="systemRoles">The roles the system identity holds. Nothing is inherited from any caller.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="OccurrenceCommandResult"/>.</returns>
    /// <exception cref="EffectReceiptsNotSupported">The kernel capability is absent or unknown.</exception>
    Task<OccurrenceCommandResult> Execute(object command, OccurrenceContext occurrence, EffectName effectName, IEnumerable<string> systemRoles, CancellationToken cancellationToken = default);
}
