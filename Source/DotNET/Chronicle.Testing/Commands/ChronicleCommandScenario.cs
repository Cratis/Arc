// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Testing.EventSequences;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Provides a self-contained command scenario that runs commands through the real Arc Chronicle command pipeline,
/// backed by an in-process in-memory event log.
/// </summary>
/// <remarks>
/// <para>
/// Extends <see cref="CommandScenario{TCommand}"/> with an in-memory <see cref="EventLogForTesting"/>.
/// No external Chronicle server or MongoDB instance is required.
/// </para>
/// <para>
/// Use <see cref="Given"/> to seed pre-existing events before the command runs, then call
/// <see cref="CommandScenario{TCommand}.Execute"/> or <see cref="CommandScenario{TCommand}.Validate"/>
/// to drive the command. After execution, use <c>ShouldHaveTailSequenceNumber</c>
/// or <c>ShouldHaveAppendedEvent&lt;TEvent&gt;</c> on <see cref="EventLog"/> to verify
/// which events were appended (extension methods provided by Chronicle's testing package).
/// </para>
/// <para>
/// The typical usage pattern:
/// <code>
/// public class when_registering_author_that_already_exists
/// {
///     readonly ChronicleCommandScenario&lt;RegisterAuthor&gt; _scenario = new();
///
///     [Fact]
///     public async Task should_not_succeed()
///     {
///         await _scenario.Given.Event(AuthorId.New(), new AuthorRegistered("Jane Austen"));
///         var result = await _scenario.Execute(new RegisterAuthor("Jane Austen"));
///         result.ShouldNotBeSuccessful();
///     }
/// }
/// </code>
/// </para>
/// </remarks>
/// <typeparam name="TCommand">The type of command under test.</typeparam>
public class ChronicleCommandScenario<TCommand> : CommandScenario<TCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleCommandScenario{TCommand}"/> class.
    /// </summary>
    public ChronicleCommandScenario()
    {
        EventLog = new EventLogForTesting(Defaults.Instance.EventTypes);
        Given = new EventScenarioGivenBuilder(EventLog);

        Services.AddSingleton<IEventTypes>(Defaults.Instance.EventTypes);
        Services.AddSingleton<IEventLog>(EventLog);
        Services.AddSingleton<IEventSequence>(EventLog);
    }

    /// <summary>
    /// Gets the fluent builder for seeding pre-existing events into the in-memory event log before the command runs.
    /// </summary>
    public EventScenarioGivenBuilder Given { get; }

    /// <summary>
    /// Gets the in-memory <see cref="EventLogForTesting"/> that acts as the event log during the test.
    /// </summary>
    /// <remarks>
    /// Use this to assert on the tail sequence number and appended events after calling
    /// <see cref="CommandScenario{TCommand}.Execute"/>.
    /// </remarks>
    public EventLogForTesting EventLog { get; }

    /// <summary>
    /// Gets the in-memory <see cref="IEventSequence"/> backed by <see cref="EventLog"/>.
    /// </summary>
    /// <remarks>
    /// Provided as a convenience alias for test authors who prefer to work in terms of
    /// <see cref="IEventSequence"/>.
    /// </remarks>
    public IEventSequence EventSequence => EventLog;
}
