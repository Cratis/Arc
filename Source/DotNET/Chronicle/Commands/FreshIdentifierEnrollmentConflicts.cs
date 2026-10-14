// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Maps enrollment conflicts against generated guards recorded on the active command's unit of work.
/// </summary>
public sealed class FreshIdentifierEnrollmentConflicts : ICommandExceptionMapper
{
    /// <inheritdoc/>
    public bool TryMap(CommandContext context, Exception exception, [NotNullWhen(true)] out CommandResult? result)
    {
        result = null;
        if (exception is not ConflictingEnrollmentScope conflict ||
            conflict.EventSequenceId is not { } sequence ||
            !CommandTransaction.TryGetActive(out var unit) ||
            FreshIdentifierScopes.For(unit)?.Contains(sequence, conflict.EventSourceId) != true ||
            !FreshIdentifierScopes.IsWholeSourceBeforeFirst(conflict.Required, conflict.EventSourceId) ||
            conflict.Effective == ConcurrencyScope.NotSet ||
            FreshIdentifierScopes.IsWholeSourceBeforeFirst(conflict.Effective, conflict.EventSourceId))
        {
            return false;
        }

        result = CommandResult.Error(context.CorrelationId, new ConflictingConcurrencyScopes());

        return true;
    }
}
