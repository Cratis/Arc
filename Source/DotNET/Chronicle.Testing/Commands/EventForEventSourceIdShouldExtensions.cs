// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Provides assertions over returned decisions routed to event sources.
/// </summary>
/// <remarks>
/// These assert the returned decision, not what was appended. Use the ShouldHaveAppendedEvent helpers on a
/// CommandScenario to prove appends. Import this namespace to select these overloads instead of the object helpers.
/// For a Result, chain ShouldBeAccepted().ShouldTarget(id).ShouldBeEvent&lt;TEvent&gt;().
/// </remarks>
public static class EventForEventSourceIdShouldExtensions
{
    /// <summary>
    /// Asserts that the returned event targets the specified source.
    /// </summary>
    /// <param name="routed">The returned routed event.</param>
    /// <param name="eventSourceId">The expected event source.</param>
    /// <returns>The routed event.</returns>
    /// <exception cref="CommandDecisionAssertionException">The event is null or targets another source.</exception>
    public static EventForEventSourceId ShouldTarget(this EventForEventSourceId? routed, EventSourceId eventSourceId)
    {
        if (!Targets(routed, eventSourceId))
        {
            throw new CommandDecisionAssertionException(
                $"Expected command to target '{eventSourceId}', but it did not. Actual: {Describe(routed)}.");
        }

        return routed!;
    }

    /// <summary>
    /// Asserts the exact type and optional predicate of the returned routed payload.
    /// </summary>
    /// <typeparam name="TEvent">The exact event type.</typeparam>
    /// <param name="routed">The returned routed event.</param>
    /// <param name="predicate">An optional condition on the payload.</param>
    /// <param name="predicateExpression">The predicate expression captured at the call site.</param>
    /// <returns>The matched payload.</returns>
    /// <exception cref="CommandDecisionAssertionException">The payload does not match.</exception>
    public static TEvent ShouldBeEvent<TEvent>(this EventForEventSourceId? routed, Func<TEvent, bool>? predicate = null, [CallerArgumentExpression(nameof(predicate))] string predicateExpression = "") =>
        routed is null ? NullEvent<TEvent>() : routed.Event.ShouldBeEvent(predicate, predicateExpression);

    /// <summary>
    /// Asserts the assignable type and optional predicate of the returned routed payload.
    /// </summary>
    /// <typeparam name="TEvent">The event type or interface to accept.</typeparam>
    /// <param name="routed">The returned routed event.</param>
    /// <param name="predicate">An optional condition on the payload.</param>
    /// <param name="predicateExpression">The predicate expression captured at the call site.</param>
    /// <returns>The matched payload.</returns>
    /// <exception cref="CommandDecisionAssertionException">The payload does not match.</exception>
    public static TEvent ShouldBeEventAssignableTo<TEvent>(this EventForEventSourceId? routed, Func<TEvent, bool>? predicate = null, [CallerArgumentExpression(nameof(predicate))] string predicateExpression = "") =>
        routed is null ? NullEvent<TEvent>() : routed.Event.ShouldBeEventAssignableTo(predicate, predicateExpression);

    /// <summary>
    /// Asserts that a returned routed collection contains an exact event type at any source.
    /// </summary>
    /// <typeparam name="TEvent">The exact event type.</typeparam>
    /// <param name="routed">The returned routed events.</param>
    /// <param name="predicate">An optional condition on the payload.</param>
    /// <param name="predicateExpression">The predicate expression captured at the call site.</param>
    /// <returns>The first matching payload.</returns>
    /// <exception cref="CommandDecisionAssertionException">No payload matches.</exception>
    public static TEvent ShouldContainEvent<TEvent>(this IEnumerable<EventForEventSourceId>? routed, Func<TEvent, bool>? predicate = null, [CallerArgumentExpression(nameof(predicate))] string predicateExpression = "") =>
        AssertContains(routed, null, (predicate, predicateExpression), false);

    /// <summary>
    /// Asserts that a returned routed collection contains an exact event type at the specified source.
    /// </summary>
    /// <typeparam name="TEvent">The exact event type.</typeparam>
    /// <param name="routed">The returned routed events.</param>
    /// <param name="eventSourceId">The expected event source.</param>
    /// <param name="predicate">An optional condition on the payload.</param>
    /// <param name="predicateExpression">The predicate expression captured at the call site.</param>
    /// <returns>The first matching payload at the source.</returns>
    /// <exception cref="CommandDecisionAssertionException">No payload matches at the source.</exception>
    public static TEvent ShouldContainEvent<TEvent>(this IEnumerable<EventForEventSourceId>? routed, EventSourceId eventSourceId, Func<TEvent, bool>? predicate = null, [CallerArgumentExpression(nameof(predicate))] string predicateExpression = "") =>
        AssertContains(routed, eventSourceId, (predicate, predicateExpression), false);

    /// <summary>
    /// Asserts that a returned routed collection contains an assignable event at the specified source.
    /// </summary>
    /// <typeparam name="TEvent">The event type or interface to accept.</typeparam>
    /// <param name="routed">The returned routed events.</param>
    /// <param name="eventSourceId">The expected event source.</param>
    /// <param name="predicate">An optional condition on the payload.</param>
    /// <param name="predicateExpression">The predicate expression captured at the call site.</param>
    /// <returns>The first matching payload at the source.</returns>
    /// <exception cref="CommandDecisionAssertionException">No payload matches at the source.</exception>
    public static TEvent ShouldContainEventAssignableTo<TEvent>(this IEnumerable<EventForEventSourceId>? routed, EventSourceId eventSourceId, Func<TEvent, bool>? predicate = null, [CallerArgumentExpression(nameof(predicate))] string predicateExpression = "") =>
        AssertContains(routed, eventSourceId, (predicate, predicateExpression), true);

    static bool Targets(EventForEventSourceId? routed, EventSourceId source) => routed?.Event is not null && routed.EventSourceId == source;

    static TEvent AssertContains<TEvent>(IEnumerable<EventForEventSourceId>? routed, EventSourceId? source, (Func<TEvent, bool>? Predicate, string Expression) filter, bool assignable)
    {
        var values = routed?.ToArray();
        var candidates = (values ?? []).Where(value => value is not null && (source is null || value.EventSourceId == source));
        var matches = candidates.Where(value => CommandDecisionValues.Matches<TEvent>(value.Event, assignable)).Select(value => (TEvent)value.Event);
        foreach (var match in matches.Where(value => filter.Predicate is null || filter.Predicate(value)))
        {
            return match;
        }

        var actual = values is null ? "<null>" : DescribeAll(values);
        throw new CommandDecisionAssertionException(
            $"Expected command to contain event '{typeof(TEvent).Name}' at '{source?.ToString() ?? "any source"}' matching '{filter.Expression}', but none matched. Actual: {actual}{CommandDecisionValues.AssignableHint<TEvent>(assignable, "ShouldContainEvent")}.");
    }

    static TEvent NullEvent<TEvent>() => throw new CommandDecisionAssertionException("Expected command to return a routed event, but it was <null>.");

    static string Describe(EventForEventSourceId? routed) => routed is null ? "<null>" : $"{routed.EventSourceId}: {CommandDecisionValues.Describe(routed.Event)}";

    static string DescribeAll(EventForEventSourceId[] values) => values.Length == 0
        ? "no values"
        : string.Join(", ", values.Select((value, index) => $"[{index}] {Describe(value)}"));
}
