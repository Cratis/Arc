// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Testing.Events;
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
        /// Gets the in-memory <see cref="EventLogForTesting"/> that acts as the event log during the test.
        /// </summary>
        /// <remarks>
        /// Use this to assert on the tail sequence number and appended events after calling
        /// <see cref="CommandScenario{TCommand}.Execute"/>.
        /// </remarks>
        public EventLogForTesting EventLog =>
            (EventLogForTesting)scenario.Context[typeof(EventLogForTesting)];

        /// <summary>
        /// Gets the fluent builder for seeding pre-existing events into the in-memory event log before the command runs.
        /// </summary>
        public EventScenarioGivenBuilder Given =>
            (EventScenarioGivenBuilder)scenario.Context[typeof(EventScenarioGivenBuilder)];
    }
}
