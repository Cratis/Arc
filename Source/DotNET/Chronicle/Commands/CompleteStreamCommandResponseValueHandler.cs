// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Enrolls a returned stream completion to run after the command's events have committed.
/// </summary>
/// <param name="eventLog">The event log for the command's scope.</param>
public class CompleteStreamCommandResponseValueHandler(IEventLog eventLog) : ICommandResponseValueHandler, ICommandResponseValueHandler<CompleteStream>
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value) => value is CompleteStream;

    /// <inheritdoc/>
    public Task<CommandResult> Handle(CommandContext commandContext, object value)
    {
        var completion = (CompleteStream)value;
        var streamType = completion.EventStreamType ?? commandContext.GetEventStreamType() ?? EventStreamType.All;
        var streamId = completion.EventStreamId ?? commandContext.GetEventStreamId() ?? EventStreamId.Default;
        var result = CommandResult.Success(commandContext.CorrelationId);
        if (streamType.IsAll && (streamId.IsDefault || streamId == EventStreamId.NotSet))
        {
            result.ValidationResults = [ValidationResult.Error("The default stream cannot be completed.")];
        }
        else if (!TransactionalCommandScope.EnrollCompletion(commandContext, new PendingStreamCompletion(eventLog, streamType, streamId)))
        {
            result.ExceptionMessages = ["Stream completion requires an active command execution scope."];
        }

        return Task.FromResult(result);
    }
}
