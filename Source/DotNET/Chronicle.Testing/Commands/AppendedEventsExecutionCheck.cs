// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Records the append boundary before each command execution, excluding seeded and earlier events.
/// </summary>
/// <param name="context">The scenario context containing the current append capture.</param>
internal sealed class AppendedEventsExecutionCheck(IDictionary<string, object> context) : ICommandScenarioExecutionCheck
{
    /// <summary>
    /// Gets the number of captured append attempts before the latest command started.
    /// </summary>
    public int CountBeforeExecution { get; private set; }

    /// <inheritdoc/>
    public void Verify(Type commandType, ScenarioGeneratedValueFixture fixture) =>
        CountBeforeExecution = ((List<AppendedEventWithResult>)context[ChronicleCommandScenarioExtender.AppendedEventsKey]).Count;
}
