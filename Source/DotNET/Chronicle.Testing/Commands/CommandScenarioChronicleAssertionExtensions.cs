// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Provides assertion extension methods for <see cref="CommandScenario{TCommand}"/> that verify
/// which events were appended by a command handler.
/// </summary>
/// <remarks>
/// These assertions operate on the events captured via the <c>AppendOperations</c> observable on
/// the client-side event log, which is more reliable than reading back through the in-process
/// kernel storage layer.
/// </remarks>
public static class CommandScenarioChronicleAssertionExtensions
{
    /// <summary>
    /// Asserts that at least one event of the specified type was appended for the given event source.
    /// </summary>
    /// <typeparam name="TCommand">The command type of the scenario.</typeparam>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="scenario">The <see cref="CommandScenario{TCommand}"/> to assert on.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to filter by.</param>
    /// <returns>A completed <see cref="Task"/>.</returns>
    /// <exception cref="EventSequenceAssertionException">Thrown when no matching event is found.</exception>
    public static Task ShouldHaveAppendedEvent<TCommand, TEvent>(
        this CommandScenario<TCommand> scenario,
        EventSourceId eventSourceId)
    {
        var appendedEvents = (List<AppendedEventWithResult>)scenario.Context[ChronicleCommandScenarioExtender.AppendedEventsKey];
        _ = appendedEvents.FirstOrDefault(e =>
            e.Event.Content is TEvent &&
            e.Event.Context.EventSourceId == eventSourceId)
            ?? throw new EventSequenceAssertionException(
                $"Expected at least one event of type '{typeof(TEvent).Name}' for event source '{eventSourceId}', but none was found. " +
                $"Total events appended: {appendedEvents.Count}.");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Asserts that at least one event of the specified type was appended for the given event source
    /// and that the event matches the predicate.
    /// </summary>
    /// <typeparam name="TCommand">The command type of the scenario.</typeparam>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="scenario">The <see cref="CommandScenario{TCommand}"/> to assert on.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to filter by.</param>
    /// <param name="predicate">A function that returns <see langword="true"/> if the event matches expectations.</param>
    /// <returns>A completed <see cref="Task"/>.</returns>
    /// <exception cref="EventSequenceAssertionException">Thrown when no matching event is found.</exception>
    public static Task ShouldHaveAppendedEvent<TCommand, TEvent>(
        this CommandScenario<TCommand> scenario,
        EventSourceId eventSourceId,
        Func<TEvent, bool> predicate)
    {
        var appendedEvents = (List<AppendedEventWithResult>)scenario.Context[ChronicleCommandScenarioExtender.AppendedEventsKey];
        _ = appendedEvents
            .Where(e => e.Event.Content is TEvent && e.Event.Context.EventSourceId == eventSourceId)
            .Select(e => (TEvent)e.Event.Content)
            .FirstOrDefault(predicate)
            ?? throw new EventSequenceAssertionException(
                $"Expected at least one event of type '{typeof(TEvent).Name}' for event source '{eventSourceId}' matching the predicate, but none was found. " +
                $"Total events appended: {appendedEvents.Count}.");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Asserts that the tail sequence number of the events appended during command execution
    /// matches the expected value.
    /// </summary>
    /// <typeparam name="TCommand">The command type of the scenario.</typeparam>
    /// <param name="scenario">The <see cref="CommandScenario{TCommand}"/> to assert on.</param>
    /// <param name="expected">The expected tail <see cref="EventSequenceNumber"/>.</param>
    /// <returns>A completed <see cref="Task"/>.</returns>
    /// <exception cref="EventSequenceAssertionException">Thrown when the tail sequence number does not match.</exception>
    public static Task ShouldHaveTailSequenceNumber<TCommand>(
        this CommandScenario<TCommand> scenario,
        EventSequenceNumber expected)
    {
        var appendedEvents = (List<AppendedEventWithResult>)scenario.Context[ChronicleCommandScenarioExtender.AppendedEventsKey];

        if (appendedEvents.Count == 0)
        {
            throw new EventSequenceAssertionException(
                $"Expected tail sequence number {expected}, but no events were appended.");
        }

        var actual = appendedEvents.Max(e => e.Result.SequenceNumber);
        if (actual != expected)
        {
            throw new EventSequenceAssertionException(
                $"Expected tail sequence number to be {expected}, but it was {actual}.");
        }

        return Task.CompletedTask;
    }
}
