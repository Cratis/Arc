// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Provides Chronicle-specific extension properties for <see cref="CommandScenario{TCommand}"/>.
/// </summary>
/// <remarks>
/// These extension properties are available on any <see cref="CommandScenario{TCommand}"/> instance
/// when the <c>Cratis.Arc.Chronicle.Testing</c> package is referenced and
/// <see cref="ChronicleCommandScenarioExtender"/> has populated the scenario context.
/// </remarks>
public static class CommandScenarioChronicleExtensions
{
    extension<TCommand>(CommandScenario<TCommand> scenario)
    {
        /// <summary>
        /// Gets the <see cref="EventScenario"/> that provides the in-memory event log and
        /// a fluent builder for seeding pre-existing events.
        /// </summary>
        /// <remarks>
        /// Use <see cref="EventScenario.Given"/> to seed events before the command runs.
        /// </remarks>
        public EventScenario EventScenario =>
            (EventScenario)scenario.Context[ChronicleCommandScenarioExtender.ContextKey];

        /// <summary>
        /// Gets the <see cref="IEventLog"/> from the in-process Chronicle event scenario.
        /// </summary>
        public IEventLog EventLog =>
            ((EventScenario)scenario.Context[ChronicleCommandScenarioExtender.ContextKey]).EventLog;

        /// <summary>
        /// Gets the <see cref="IEventSequence"/> from the in-process Chronicle event scenario.
        /// </summary>
        public IEventSequence EventSequence =>
            ((EventScenario)scenario.Context[ChronicleCommandScenarioExtender.ContextKey]).EventSequence;

        /// <summary>
        /// Gets the events appended to the event log during command execution, captured via the
        /// <c>AppendOperations</c> observable on the client-side event log.
        /// </summary>
        /// <remarks>
        /// Use the assertion helpers in <see cref="CommandScenarioChronicleAssertionExtensions"/>
        /// to assert on these captured events.
        /// </remarks>
        public IReadOnlyList<AppendedEventWithResult> AppendedEvents =>
            (List<AppendedEventWithResult>)scenario.Context[ChronicleCommandScenarioExtender.AppendedEventsKey];
    }
}
