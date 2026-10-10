// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Merges event tags returned by a command into its context before returned events are appended.
/// </summary>
public class EventTagsCommandResponseValueHandler : ICommandResponseValueContextUpdater, ICommandResponseValueHandler<EventTags>
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value) => value is EventTags;

    /// <inheritdoc/>
    public void UpdateContext(CommandContext commandContext, object value)
    {
        if (value is EventTags eventTags)
        {
            var tags = commandContext.GetEventTags().Concat(eventTags.Tags).DistinctBy(_ => (_.Name.Value, _.Value)).ToArray();
            if (tags.Length > 0)
            {
                commandContext.Values[WellKnownCommandContextKeys.EventTags] = tags;
            }
        }
    }

    /// <inheritdoc/>
    public Task<CommandResult> Handle(CommandContext commandContext, object value) =>
        Task.FromResult(CommandResult.Success(commandContext.CorrelationId));
}
