// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Occurrences;

namespace Cratis.Arc.Chronicle.Occurrences;

/// <summary>
/// Represents the result of executing a command for an occurrence effect.
/// </summary>
/// <param name="Disposition">How the execution settled.</param>
/// <param name="Outcome">The durable outcome of the effect, or null when the attempt <see cref="OccurrenceCommandDisposition.Failed"/>.</param>
/// <param name="Response">The plaintext, personal-data-free response stored with the receipt, or null.</param>
/// <param name="CommandResult">The result of the command run by this attempt, or null when a stored receipt was returned instead.</param>
public record OccurrenceCommandResult(OccurrenceCommandDisposition Disposition, EffectOutcome? Outcome, JsonNode? Response, CommandResult? CommandResult)
{
    /// <summary>
    /// Gets whether the effect is settled, meaning a durable receipt exists and the occurrence must not run it again.
    /// </summary>
    public bool IsSettled => Disposition != OccurrenceCommandDisposition.Failed;
}
