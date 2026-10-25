// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences;

namespace Cratis.Arc.Chronicle.Testing.Slices;

/// <summary>
/// Provides the outcome and assertion vocabulary of a completed slice.
/// </summary>
/// <remarks>
/// The assertion members are a compatibility contract mirrored from
/// <see cref="CommandResultShouldExtensions"/>, <see cref="CommandScenarioAppendedEventsAssertionExtensions"/>,
/// and the ShouldHaveAppendedEvent overloads on <see cref="CommandScenarioChronicleAssertionExtensions"/>.
/// Their names, parameters, return types, and assertion behavior match the underlying extensions.
/// </remarks>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <param name="result">The command result.</param>
/// <param name="scenario">The scenario that executed the command.</param>
public sealed class SliceOutcome<TCommand>(CommandResult result, CommandScenario<TCommand> scenario)
    where TCommand : class
{
    /// <summary>
    /// Gets the command result for assertions beyond the mirrored compatibility contract.
    /// </summary>
    public CommandResult Result { get; } = result;

    /// <summary>
    /// Gets the scenario for assertions beyond the mirrored compatibility contract.
    /// </summary>
    public CommandScenario<TCommand> Scenario { get; } = scenario;

    /// <summary>
    /// Asserts command success using the mirrored command result assertion.
    /// </summary>
    /// <exception cref="CommandResultAssertionException">The command was unsuccessful.</exception>
    public void ShouldBeSuccessful() => Result.ShouldBeSuccessful();

    /// <summary>
    /// Asserts rejection using the mirrored command result assertion.
    /// </summary>
    /// <exception cref="CommandResultAssertionException">The command was not rejected, or only a dependency rejected it.</exception>
    public void ShouldBeRejected() => Result.ShouldBeRejected();

    /// <summary>
    /// Asserts an exact rejection message using the mirrored command result assertion.
    /// </summary>
    /// <param name="reason">The exact validation message to match.</param>
    /// <exception cref="CommandResultAssertionException">No validation message matches the reason.</exception>
    public void ShouldBeRejected(string reason) => Result.ShouldBeRejected(reason);

    /// <summary>
    /// Asserts authorization denial using the mirrored command result assertion.
    /// </summary>
    /// <exception cref="CommandResultAssertionException">The command was authorized.</exception>
    public void ShouldBeDenied() => Result.ShouldBeDenied();

    /// <summary>
    /// Asserts that no events were appended using the mirrored scenario assertion.
    /// </summary>
    /// <returns>The underlying assertion task.</returns>
    /// <exception cref="EventSequenceAssertionException">The command appended events.</exception>
    /// <exception cref="CommandResultAssertionException">No command has been executed.</exception>
    public Task ShouldNotHaveAppendedEvents() => Scenario.ShouldNotHaveAppendedEvents();

    /// <summary>
    /// Asserts that no events were appended to a source using the mirrored scenario assertion.
    /// </summary>
    /// <param name="eventSourceId">The event source to check.</param>
    /// <returns>The underlying assertion task.</returns>
    /// <exception cref="EventSequenceAssertionException">The command appended events to the source.</exception>
    /// <exception cref="CommandResultAssertionException">No command has been executed.</exception>
    public Task ShouldNotHaveAppendedEvents(EventSourceId eventSourceId) => Scenario.ShouldNotHaveAppendedEvents(eventSourceId);

    /// <summary>
    /// Asserts exact event values in order using the mirrored scenario assertion.
    /// </summary>
    /// <param name="events">The complete ordered list of expected event values.</param>
    /// <returns>The underlying assertion task.</returns>
    /// <exception cref="EventSequenceAssertionException">The appended events differ.</exception>
    /// <exception cref="CommandResultAssertionException">No command has been executed.</exception>
    public Task ShouldHaveAppendedEventsInOrder(params object[] events) => Scenario.ShouldHaveAppendedEventsInOrder(events);

    /// <summary>
    /// Asserts exact event values in any order using the mirrored scenario assertion.
    /// </summary>
    /// <param name="events">The complete list of expected event values, including duplicates.</param>
    /// <returns>The underlying assertion task.</returns>
    /// <exception cref="EventSequenceAssertionException">The appended events differ.</exception>
    /// <exception cref="CommandResultAssertionException">No command has been executed.</exception>
    public Task ShouldHaveAppendedEventsInAnyOrder(params object[] events) => Scenario.ShouldHaveAppendedEventsInAnyOrder(events);

    /// <summary>
    /// Asserts that an event type was appended for a source using the mirrored scenario assertion.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="eventSourceId">The event source to check.</param>
    /// <returns>The underlying assertion task.</returns>
    /// <exception cref="EventSequenceAssertionException">No matching event is found.</exception>
    public Task ShouldHaveAppendedEvent<TEvent>(EventSourceId eventSourceId) => Scenario.ShouldHaveAppendedEvent<TCommand, TEvent>(eventSourceId);

    /// <summary>
    /// Asserts that an event matches a predicate using the mirrored scenario assertion.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="eventSourceId">The event source to check.</param>
    /// <param name="predicate">The predicate the event must satisfy.</param>
    /// <returns>The underlying assertion task.</returns>
    /// <exception cref="EventSequenceAssertionException">No matching event is found.</exception>
    public Task ShouldHaveAppendedEvent<TEvent>(EventSourceId eventSourceId, Func<TEvent, bool> predicate) => Scenario.ShouldHaveAppendedEvent<TCommand, TEvent>(eventSourceId, predicate);
}
