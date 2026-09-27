// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
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
        /// Use <see cref="EventScenario.Given"/> to seed events before the command runs in legacy mode.
        /// </remarks>
        /// <exception cref="NotSupportedException">The decision-mode store cannot be represented as an EventScenario.</exception>
        public EventScenario EventScenario =>
            scenario.Context.ContainsKey(ChronicleCommandScenarioExtender.DecisionScenarioKey)
                ? throw new NotSupportedException("EventScenario uses a separate log. In decision mode use EventLog and Given.ForEventSource(...).Events(...) instead.")
                : (EventScenario)scenario.Context[ChronicleCommandScenarioExtender.ContextKey];

        /// <summary>
        /// Gets the <see cref="IEventLog"/> from the in-process Chronicle event scenario.
        /// </summary>
        public IEventLog EventLog =>
            scenario.Context.TryGetValue(ChronicleCommandScenarioExtender.DecisionScenarioKey, out var decision)
                ? ((DecisionCommandScenario)decision).Store.EventLog
                : ((EventScenario)scenario.Context[ChronicleCommandScenarioExtender.ContextKey]).EventLog;

        /// <summary>
        /// Gets the <see cref="IEventSequence"/> from the in-process Chronicle event scenario.
        /// </summary>
        public IEventSequence EventSequence =>
            scenario.Context.TryGetValue(ChronicleCommandScenarioExtender.DecisionScenarioKey, out var decision)
                ? ((DecisionCommandScenario)decision).Store.EventLog
                : ((EventScenario)scenario.Context[ChronicleCommandScenarioExtender.ContextKey]).EventSequence;

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

        /// <summary>
        /// Gets the Chronicle-specific given builder for setting up command scenario state.
        /// </summary>
        public CommandScenarioChronicleGivenBuilder<TCommand> Given =>
            new(scenario);

        /// <summary>Opts into one real in-process event log for seeded events, decision reads and command commits.</summary>
        /// <remarks>Call before seeding events or executing. The legacy EventScenario and pinned read models cannot be used in this mode.</remarks>
        /// <exception cref="InvalidOperationException">The scenario has already initialized or legacy state has been seeded.</exception>
        /// <exception cref="NotSupportedException">Custom execution scopes cannot be safely ordered around the owner.</exception>
        public CommandScenario<TCommand> UseDecisionReads()
        {
            if (scenario.IsInitialized) throw new InvalidOperationException("Enable decision reads before the first Execute or Validate call.");
            ChronicleCommandScenarioExtender.EnableDecisionReads(scenario.Services, scenario.Context);
            return scenario;
        }

        /// <summary>Queues competing facts to append after the handler reads and before its owner commits.</summary>
        /// <param name="eventSourceId">The source being changed by a competitor.</param>
        /// <param name="events">The competing events.</param>
        /// <exception cref="NotSupportedException">Protected decision mode was not enabled.</exception>
        public void AppendConcurrently(EventSourceId eventSourceId, params object[] events)
        {
            if (!scenario.Context.TryGetValue(ChronicleCommandScenarioExtender.DecisionScenarioKey, out var decision))
                throw new NotSupportedException("AppendConcurrently requires UseDecisionReads().");
            ((DecisionCommandScenario)decision).QueueCompetingAppend(eventSourceId, events);
        }
    }
}
