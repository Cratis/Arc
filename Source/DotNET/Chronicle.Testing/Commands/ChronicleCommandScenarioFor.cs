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
/// Base class for command scenario specifications that run commands through the real Arc Chronicle command pipeline,
/// backed by an in-process in-memory event log.
/// </summary>
/// <remarks>
/// <para>
/// Builds on <see cref="CommandScenarioFor{TCommand}"/> and wires the real Arc Chronicle infrastructure
/// (event-type discovery, response value handlers, context values providers) against an in-memory
/// <see cref="EventLogForTesting"/> so that no external Chronicle server is required.
/// </para>
/// <para>
/// Use <see cref="Given"/> to seed pre-existing events before the command runs, then call
/// <see cref="CommandScenarioFor{TCommand}.Execute"/> or <see cref="CommandScenarioFor{TCommand}.Validate"/>
/// to drive the command.  After execution, inspect <see cref="EventLog"/> and <see cref="EventSequence"/>
/// to assert on the events that were appended.
/// </para>
/// </remarks>
/// <typeparam name="TCommand">The type of command under test.</typeparam>
public abstract class ChronicleCommandScenarioFor<TCommand> : CommandScenarioFor<TCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleCommandScenarioFor{TCommand}"/> class.
    /// </summary>
    protected ChronicleCommandScenarioFor()
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
    /// <see cref="CommandScenarioFor{TCommand}.Execute"/>.
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
