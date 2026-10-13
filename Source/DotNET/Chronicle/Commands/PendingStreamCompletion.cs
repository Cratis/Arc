// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Holds a completion until the owning command has successfully committed its events.
/// </summary>
/// <param name="EventLog">The command's event log.</param>
/// <param name="StreamType">The resolved stream type.</param>
/// <param name="StreamId">The resolved stream id.</param>
/// <param name="Scope">The owning event source scope, or null for an all-sources completion.</param>
internal sealed record PendingStreamCompletion(IEventLog EventLog, EventStreamType StreamType, EventStreamId StreamId, ClosedStreamScope? Scope)
{
    /// <summary>
    /// Applies the completion and merges any framework failure into the command result.
    /// </summary>
    /// <param name="result">The command result.</param>
    /// <returns>A task representing completion.</returns>
    internal async Task Complete(CommandResult result)
    {
        var completion = Scope is null
            ? await EventLog.CompleteStream(StreamType, StreamId)
            : await EventLog.CompleteStream(Scope);
        if (!completion.IsSuccess && (!completion.TryGetError(out var error) || error != CompleteStreamError.AlreadyCompleted))
        {
            result.ValidationResults = [.. result.ValidationResults, ValidationResult.Error(
                $"Stream '{StreamType}/{StreamId}' could not be completed: {error}.", reason: ValidationResultReason.FrameworkFailure)];
        }
    }
}
