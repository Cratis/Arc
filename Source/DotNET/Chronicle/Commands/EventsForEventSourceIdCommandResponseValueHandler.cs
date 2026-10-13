// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents a command response value handler that can handle a collection containing one or more
/// <see cref="EventForEventSourceId"/> wrappers as the response value — including a mixed collection of
/// plain events and wrappers.
/// </summary>
/// <param name="eventLog">The event log to append events to.</param>
/// <param name="eventTypes">The event types.</param>
/// <param name="concurrencyScopeStrategies">The <see cref="IConcurrencyScopeStrategies"/> for resolving the expected sequence number.</param>
/// <param name="eventSources">The registered event source definitions, or null to skip legacy routing validation.</param>
/// <remarks>
/// The match is based on the runtime type of each element rather than the static type of the collection, so a
/// collection boxed as <see cref="IEnumerable{T}"/> of <see cref="object"/> — or a mixed collection of plain events
/// and <see cref="EventForEventSourceId"/> wrappers — is appended instead of falling through the handlers and being
/// silently serialized as the response payload. Each wrapper is appended to its own event source id; each plain event
/// is appended to the command's event source id.
/// <para>
/// Without an active command transaction, a collection is appended as one batch, so a refusal appends nothing.
/// The exception is an event source id with no resolved concurrency guard whose events target several routes:
/// those events append one at a time because Chronicle carries only one guard per event source id in a batch.
/// </para>
/// </remarks>
[method: ActivatorUtilitiesConstructor]
public class EventsForEventSourceIdCommandResponseValueHandler(
    IEventLog eventLog,
    IEventTypes eventTypes,
    IConcurrencyScopeStrategies concurrencyScopeStrategies,
    IEventSources? eventSources) : ICommandResponseValueHandler, ICommandResponseValueHandler<IEnumerable<EventForEventSourceId>>
{
    /// <summary>
    /// Initializes a handler without validation of per-event routing against event source definitions.
    /// </summary>
    /// <param name="eventLog">The event log.</param>
    /// <param name="eventTypes">The registered event types.</param>
    /// <param name="concurrencyScopeStrategies">The concurrency scope strategies.</param>
    /// <remarks>Use the constructor taking <see cref="IEventSources"/> to validate legacy wrapper routes.</remarks>
    [Obsolete("This constructor does not validate per-event routing against event source definitions; use the constructor that takes IEventSources.")]
    public EventsForEventSourceIdCommandResponseValueHandler(IEventLog eventLog, IEventTypes eventTypes, IConcurrencyScopeStrategies concurrencyScopeStrategies)
        : this(eventLog, eventTypes, concurrencyScopeStrategies, null)
    {
    }

    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value)
    {
        if (value is not IEnumerable enumerable || value is string)
        {
            return false;
        }

        // Validate every element in a single pass, without materializing the collection: each element must be an
        // EventForEventSourceId wrapper carrying a registered event, or a registered plain event. Short-circuit on
        // the first invalid element.
        var hasItems = false;
        var hasWrapper = false;
        foreach (var item in enumerable)
        {
            hasItems = true;
            if (item is EventForEventSourceId wrapper)
            {
                hasWrapper = true;
                if (!eventTypes.HasFor(wrapper.Event.GetType()))
                {
                    return false;
                }
            }
            else if (item is null || !eventTypes.HasFor(item.GetType()))
            {
                return false;
            }
        }

        // An empty collection statically typed as wrappers is still ours — it is recognized (and appends nothing)
        // rather than being serialized as the response payload.
        if (!hasItems)
        {
            return value is IEnumerable<EventForEventSourceId>;
        }

        // A non-empty collection must contain at least one wrapper, distinguishing it from a pure plain-event
        // collection handled by the sibling handler.
        return hasWrapper;
    }

    /// <inheritdoc/>
    /// <exception cref="EventStreamDoesNotBelongToEventSource">A legacy wrapper route names a known definition and an undeclared stream on a definition-routed command.</exception>
    public async Task<CommandResult> Handle(CommandContext commandContext, object value)
    {
        // Resolve every event's routing and guard first. Events for one event source id share one scope, so guards that
        // cannot be shared are rejected here, before the first event is enrolled or appended.
        var (targets, derivedGuards) = await ResolveTargets(commandContext, (IEnumerable)value);

        // A scope carries the expected tail of one stream, so it cannot be shared across the streams a
        // cross-stream command writes to - each target gets its own, resolved once however many events it takes.
        var concurrencyScopes = CommandConcurrencyScopes.Resolve(derivedGuards);
        var appendAsBatch = targets.Count > 1 && CanAppendAsBatch(targets, concurrencyScopes);

        foreach (var target in targets)
        {
            var concurrencyScope = concurrencyScopes[target.EventSourceId];
            if (eventLog.TryEnrollForCommand(target.EventSourceId, target.Event, commandContext, concurrencyScope, target.Tags, target.Occurred, target.Routing, target.NamedTags))
            {
                continue;
            }

            if (appendAsBatch)
            {
                return await AppendAsBatch(commandContext, targets, concurrencyScopes);
            }

            var result = await eventLog.AppendForCommand(target.EventSourceId, target.Event, target.Routing, concurrencyScope, target.Tags, target.Occurred, target.NamedTags);
            if (!result.IsSuccess)
            {
                return result.ToCommandResult();
            }
        }

        return CommandResult.Success(commandContext.CorrelationId);
    }

    static bool CanAppendAsBatch(IEnumerable<AppendTarget> targets, IReadOnlyDictionary<EventSourceId, ConcurrencyScope?> concurrencyScopes) =>
        targets.GroupBy(_ => _.EventSourceId).All(group =>
            concurrencyScopes[group.Key] is not null || group.Select(_ => RouteKey(_.Routing)).Distinct().Count() == 1);

    static (Type? EventSource, string? EventStream, EventStreamType? EventStreamType, EventStreamId? EventStreamId, EventSourceType? EventSourceType) RouteKey(EventRouting routing) =>
        routing.EventSource is not null
            ? (routing.EventSource, routing.EventStream, null, routing.EventStreamId, null)
            : (null, null, routing.EventStreamType, routing.EventStreamId, routing.EventSourceType);

    async Task<(List<AppendTarget> Targets, List<(EventSourceId EventSourceId, DerivedConcurrencyScope Derived)> DerivedGuards)> ResolveTargets(CommandContext commandContext, IEnumerable value)
    {
        var strategy = concurrencyScopeStrategies.GetFor(eventLog);
        var targets = new List<AppendTarget>();
        var derivedGuards = new List<(EventSourceId EventSourceId, DerivedConcurrencyScope Derived)>();
        foreach (var target in value.Cast<object>().Select(item => ResolveTarget(commandContext, item)))
        {
            targets.Add(target);

            // The caller's own choice is resolved once per id, however many events it takes.
            var derived = derivedGuards.Find(_ => _.EventSourceId == target.EventSourceId && _.Derived.Origin == DerivedConcurrencyScopeOrigin.Explicit).Derived
                ?? await ConcurrencyScopeBuilder.BuildFor(commandContext, strategy, target.EventSourceId, target.Routing);
            derivedGuards.Add((target.EventSourceId, derived));
        }

        return (targets, derivedGuards);
    }

    AppendTarget ResolveTarget(CommandContext commandContext, object item)
    {
        // A wrapper keeps its own tags and occurrence time; a plain event has none of its own.
        var wrapper = item as EventForEventSourceId;
        var routing = CommandTransactionAppender.ResolveRouting(wrapper, commandContext, eventSources);
        var namedTags = CommandTransactionAppender.MergeEventTags(commandContext, wrapper?.NamedTags);
        return wrapper is not null
            ? new(wrapper.EventSourceId, wrapper.Event, wrapper.SuppliedTags(), wrapper.Occurred, routing, namedTags)
            : new(commandContext.GetEventSourceId(), item, null, null, routing, namedTags);
    }

    async Task<CommandResult> AppendAsBatch(CommandContext commandContext, IEnumerable<AppendTarget> targets, IReadOnlyDictionary<EventSourceId, ConcurrencyScope?> concurrencyScopes)
    {
        var wrappers = targets.Select(_ => CommandTransactionAppender.CreateRoutedEvent(_.EventSourceId, _.Event, _.Routing, _.NamedTags, _.Tags, _.Occurred)).ToArray();
        var scopes = concurrencyScopes.ToDictionary(_ => _.Key, _ => _.Value ?? ConcurrencyScope.NotSet);
        var result = await eventLog.AppendRoutedBatch(wrappers, scopes);

        return result.IsSuccess ? CommandResult.Success(commandContext.CorrelationId) : result.ToCommandResult();
    }

    sealed record AppendTarget(EventSourceId EventSourceId, object Event, IEnumerable<string>? Tags, DateTimeOffset? Occurred, EventRouting Routing, IEnumerable<NamedTag> NamedTags);
}
