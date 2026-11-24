// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Describes a decision read that changed before commit as a validation result.
/// </summary>
internal static class ReadChanges
{
    /// <summary>
    /// The reason detail of a guarded read that changed before commit.
    /// </summary>
    internal const string ReasonDetail = "ReadChanged";

    /// <summary>
    /// Creates the validation result for a decision conflict.
    /// </summary>
    /// <param name="conflict">The <see cref="DecisionConflict"/> Chronicle reported.</param>
    /// <param name="commandType">The command that read the model.</param>
    /// <param name="enforcing">Whether read protection was enforced for the command.</param>
    /// <returns>The <see cref="ValidationResult"/> describing the change.</returns>
    /// <remarks>
    /// The result identifies the read model without exposing its resolved key or the event that changed it.
    /// </remarks>
    internal static ValidationResult For(DecisionConflict conflict, Type commandType, bool enforcing)
    {
        var message = enforcing
            ? $"{conflict.ReadModelType.Name} changed after {commandType.Name} read it. Nothing from this command was written; submit it again."
            : $"{conflict.ReadModelType.Name} changed after it was read. Read it again and resubmit.";
        var state = new { readModel = conflict.ReadModelType.Name };

        return ValidationResult.Error(
            message,
            state: state,
            reason: ValidationResultReason.ConcurrencyViolation,
            reasonDetail: enforcing ? ReasonDetail : null);
    }
}
