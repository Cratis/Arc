// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Extensions for the command context.
/// </summary>
public static class CommandContextExtensions
{
    /// <summary>
    /// Checks whether the command context has an event source id.
    /// </summary>
    /// <param name="commandContext">The command context to check.</param>
    /// <returns>True if the command context has an event source id, false otherwise.</returns>
    public static bool HasEventSourceId(this CommandContext commandContext) =>
        (commandContext.Response is not null && EventSourceExtensions.IsEventSourceIdValue(commandContext.Response)) ||
        (commandContext.Values.TryGetValue(WellKnownCommandContextKeys.EventSourceId, out var value) && value is EventSourceId);

    /// <summary>
    /// Gets the event source id from the command context values.
    /// </summary>
    /// <param name="commandContext">The command context to get the event source id from.</param>
    /// <returns>The event source id.</returns>
    /// <exception cref="MissingEventSourceIdInCommandContext">Thrown when the event source id is missing in the command context.</exception>
    public static EventSourceId GetEventSourceId(this CommandContext commandContext)
    {
        if (commandContext.Response is not null && EventSourceExtensions.IsEventSourceIdValue(commandContext.Response))
        {
            return EventSourceExtensions.ToEventSourceId(commandContext.Response);
        }

        if (commandContext.Values.TryGetValue(WellKnownCommandContextKeys.EventSourceId, out var value) && value is EventSourceId eventSourceId)
        {
            return eventSourceId;
        }

        throw new MissingEventSourceIdInCommandContext(commandContext.Command.GetType());
    }

    /// <summary>
    /// Gets the event source type from the command context values, if present.
    /// </summary>
    /// <param name="commandContext">The command context to get the event source type from.</param>
    /// <returns>The event source type, or null if not present.</returns>
    public static EventSourceType? GetEventSourceType(this CommandContext commandContext) =>
        commandContext.Values.TryGetValue(WellKnownCommandContextKeys.EventSourceType, out var value) && value is EventSourceType eventSourceType
            ? eventSourceType
            : null;

    /// <summary>
    /// Gets the event source definition type from the command context values, if present.
    /// </summary>
    /// <param name="commandContext">The command context to get the event source definition type from.</param>
    /// <returns>The event source definition type, or null if not present.</returns>
    public static Type? GetEventSource(this CommandContext commandContext) =>
        commandContext.Values.TryGetValue(WellKnownCommandContextKeys.EventSource, out var value) && value is Type eventSource
            ? eventSource
            : null;

    /// <summary>
    /// Gets the event stream declared by the event source definition from the command context values, if present.
    /// </summary>
    /// <param name="commandContext">The command context to get the declared event stream from.</param>
    /// <returns>The declared event stream, or null if not present.</returns>
    public static string? GetEventStream(this CommandContext commandContext) =>
        commandContext.Values.TryGetValue(WellKnownCommandContextKeys.EventStream, out var value) && value is string eventStream
            ? eventStream
            : null;

    /// <summary>
    /// Gets the event stream type from the command context values, if present.
    /// </summary>
    /// <param name="commandContext">The command context to get the event stream type from.</param>
    /// <returns>The event stream type, or null if not present.</returns>
    public static EventStreamType? GetEventStreamType(this CommandContext commandContext) =>
        commandContext.Values.TryGetValue(WellKnownCommandContextKeys.EventStreamType, out var value) && value is EventStreamType eventStreamType
            ? eventStreamType
            : null;

    /// <summary>
    /// Gets the event stream id, resolving command templates on first use after validation.
    /// </summary>
    /// <param name="commandContext">The command context to get the event stream id from.</param>
    /// <returns>The event stream id, or null if not present.</returns>
    /// <exception cref="UnknownEventStreamIdTemplateProperty">A template property cannot be read.</exception>
    /// <exception cref="EventStreamIdTemplatePartMissing">A template part is blank or the resolved id is a sentinel.</exception>
    /// <exception cref="InvalidEventStreamIdTemplate">The template contains malformed braces.</exception>
    /// <exception cref="AmbiguousEventStreamId">A template and provider interface are both declared.</exception>
    public static EventStreamId? GetEventStreamId(this CommandContext commandContext)
    {
        if (commandContext.Values.TryGetValue(WellKnownCommandContextKeys.EventStreamId, out var value) && value is EventStreamId eventStreamId)
        {
            return eventStreamId;
        }
        var attribute = commandContext.Command.GetType().GetCustomAttribute<EventStreamIdAttribute>(false);
        if (attribute is null || !EventStreamIdTemplate.IsTemplate(attribute.Value.Value))
        {
            return null;
        }
        var resolved = EventStreamIdTemplate.ResolveFor(commandContext.Command);
        if (resolved is not null)
        {
            commandContext.Values[WellKnownCommandContextKeys.EventStreamId] = resolved;
        }

        return resolved;
    }

    /// <summary>
    /// Gets the resolved event route shared by the command's reads and returned events.
    /// </summary>
    /// <param name="commandContext">The command context.</param>
    /// <returns>The resolved route, with Chronicle defaults for undeclared dimensions.</returns>
    public static EventRoute GetEventRoute(this CommandContext commandContext) => new(
        commandContext.GetEventSourceType() ?? EventSourceType.Default,
        commandContext.GetEventStreamType() ?? EventStreamType.All,
        commandContext.GetEventStreamId() ?? EventStreamId.Default)
    {
        EventSource = commandContext.GetEventSource(),
        EventStream = commandContext.GetEventStream(),
        Concurrency = commandContext.Values.TryGetValue(WellKnownCommandContextKeys.ConcurrencyDimensions, out var dimensions) && dimensions is ConcurrencyDimensions concurrency
            ? concurrency
            : ConcurrencyDimensions.None
    };

    /// <summary>
    /// Gets event tags returned from the command handler and captured by the context updater.
    /// </summary>
    /// <param name="commandContext">The command context.</param>
    /// <returns>The returned named tags, or an empty collection when unset.</returns>
    /// <remarks>
    /// This accessor does not evaluate attributes or tag providers. Use <see cref="CommandEventTags.ResolveEventTags"/>
    /// at the append boundary to union all command tag sources.
    /// </remarks>
    public static IEnumerable<NamedTag> GetEventTags(this CommandContext commandContext) =>
        commandContext.Values.TryGetValue(WellKnownCommandContextKeys.EventTags, out var value) && value is IEnumerable<NamedTag> tags
            ? tags
            : [];

    /// <summary>
    /// Gets the subject from the command context, if present.
    /// </summary>
    /// <remarks>
    /// The subject is first looked up in the command response (when a handler returns a <see cref="Subject"/> directly),
    /// then in the context values (when resolved via <see cref="SubjectValuesProvider"/>).
    /// </remarks>
    /// <param name="commandContext">The command context to get the subject from.</param>
    /// <returns>The <see cref="Subject"/>, or null if not present.</returns>
    public static Subject? GetSubject(this CommandContext commandContext)
    {
        if (commandContext.Response is Subject subjectResponse)
        {
            return subjectResponse;
        }

        return commandContext.Values.TryGetValue(WellKnownCommandContextKeys.Subject, out var value) && value is Subject subject
            ? subject
            : null;
    }
}
