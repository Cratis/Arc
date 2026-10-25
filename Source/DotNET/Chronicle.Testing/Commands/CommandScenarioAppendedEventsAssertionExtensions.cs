// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Assertions over successful events from the latest command execution, excluding setup events.
/// </summary>
public static class CommandScenarioAppendedEventsAssertionExtensions
{
    /// <summary>
    /// Asserts that the command appended no events to any event source.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="scenario">The command scenario.</param>
    /// <returns>A completed task.</returns>
    /// <exception cref="EventSequenceAssertionException">The command appended events.</exception>
    /// <exception cref="CommandResultAssertionException">No command has been executed.</exception>
    public static Task ShouldNotHaveAppendedEvents<TCommand>(this CommandScenario<TCommand> scenario)
        where TCommand : class => AssertNoEvents(CommandEvents(scenario));

    /// <summary>
    /// Asserts that the command appended no events to the specified event source.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="scenario">The command scenario.</param>
    /// <param name="eventSourceId">The event source to check.</param>
    /// <returns>A completed task.</returns>
    /// <exception cref="EventSequenceAssertionException">The command appended events to the source.</exception>
    /// <exception cref="CommandResultAssertionException">No command has been executed.</exception>
    public static Task ShouldNotHaveAppendedEvents<TCommand>(this CommandScenario<TCommand> scenario, EventSourceId eventSourceId)
        where TCommand : class => AssertNoEvents(CommandEvents(scenario).Where(_ => _.Event.Context.EventSourceId == eventSourceId));

    /// <summary>
    /// Asserts exact event types and public property values in append order, ignoring event metadata.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="scenario">The command scenario.</param>
    /// <param name="events">The complete ordered list of expected event values.</param>
    /// <returns>A completed task.</returns>
    /// <exception cref="EventSequenceAssertionException">The appended events differ.</exception>
    /// <exception cref="CommandResultAssertionException">No command has been executed.</exception>
    public static Task ShouldHaveAppendedEventsInOrder<TCommand>(this CommandScenario<TCommand> scenario, params object[] events)
        where TCommand : class
    {
        var actual = CommandEvents(scenario).Select(_ => _.Event.Content).ToArray();
        for (var index = 0; index < Math.Max(events.Length, actual.Length); index++)
        {
            var difference = CompareAt(events, actual, index);
            if (difference is not null)
            {
                throw new EventSequenceAssertionException(
                    $"Expected events {{{TypeNames(events)}}} to be appended in order, but got {{{TypeNames(actual)}}}. " +
                    $"First difference at position {index}: {difference}.");
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Asserts exact event types and public property values in any order, preserving duplicate counts.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="scenario">The command scenario.</param>
    /// <param name="events">The complete list of expected event values.</param>
    /// <returns>A completed task.</returns>
    /// <exception cref="EventSequenceAssertionException">Expected events are missing or unexpected events exist.</exception>
    /// <exception cref="CommandResultAssertionException">No command has been executed.</exception>
    public static Task ShouldHaveAppendedEventsInAnyOrder<TCommand>(this CommandScenario<TCommand> scenario, params object[] events)
        where TCommand : class
    {
        var unexpected = CommandEvents(scenario).Select(_ => _.Event.Content).ToList();
        var missing = FindMissing(events, unexpected);
        if (missing.Count > 0 || unexpected.Count > 0)
        {
            throw new EventSequenceAssertionException(
                $"Expected events {{{TypeNames(events)}}} to be appended in any order, but " +
                $"{{missing: {Describe(missing)}; unexpected: {Describe(unexpected)}}}.");
        }

        return Task.CompletedTask;
    }

    static List<object> FindMissing(object[] expected, List<object> unmatched)
    {
        var missing = new List<object>();
        foreach (var @event in expected)
        {
            var index = unmatched.FindIndex(actual => ExpectedValueComparer.Compare(@event, actual, @event.GetType().Name) is null);
            if (index < 0)
            {
                missing.Add(@event);
            }
            else
            {
                unmatched.RemoveAt(index);
            }
        }

        return missing;
    }

    static ExpectedValueComparer.Difference? CompareAt(object[] expected, object[] actual, int index)
    {
        if (index >= expected.Length)
        {
            return new(actual[index].GetType().Name, "<missing>", actual[index]);
        }
        if (index >= actual.Length)
        {
            return new(expected[index].GetType().Name, expected[index], "<missing>");
        }
        return ExpectedValueComparer.Compare(expected[index], actual[index], expected[index].GetType().Name);
    }

    static Task AssertNoEvents(IEnumerable<AppendedEventWithResult> events)
    {
        var appended = events.ToArray();
        if (appended.Length > 0)
        {
            var descriptions = string.Join(", ", appended.Select(_ => $"{_.Event.Content.GetType().Name}@{_.Event.Context.EventSourceId}"));
            throw new EventSequenceAssertionException($"Expected no events to be appended, but {appended.Length} were: {descriptions}");
        }

        return Task.CompletedTask;
    }

    static IEnumerable<AppendedEventWithResult> CommandEvents<TCommand>(CommandScenario<TCommand> scenario)
        where TCommand : class
    {
        if (scenario.LastResult is null)
        {
            throw new CommandResultAssertionException("Execute a command before asserting appended events.");
        }

        var events = (List<AppendedEventWithResult>)scenario.Context[ChronicleCommandScenarioExtender.AppendedEventsKey];
        var check = (AppendedEventsExecutionCheck)scenario.Context[nameof(AppendedEventsExecutionCheck)];
        return events.Skip(check.CountBeforeExecution).Where(_ => _.Result.IsSuccess);
    }

    static string TypeNames(IEnumerable<object> events) => string.Join(", ", events.Select(_ => _.GetType().Name));

    static string Describe(IEnumerable<object> events) => string.Join(", ", events.Select(_ => $"{_.GetType().Name} {JsonSerializer.Serialize(_, _.GetType())}"));
}
