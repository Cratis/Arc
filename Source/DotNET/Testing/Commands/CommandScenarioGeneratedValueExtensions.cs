// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// Provides named generated value pins and captures for command scenarios.
/// </summary>
public static class CommandScenarioGeneratedValueExtensions
{
    extension<TCommand>(CommandScenario<TCommand> scenario)
    {
        /// <summary>
        /// Gets validated generated value captures across executions and nested commands.
        /// </summary>
        public ScenarioGeneratedValues GeneratedValues => FixtureFor(scenario).Values;

        /// <summary>
        /// Pins a generated parameter for the scenario's command type before execution.
        /// </summary>
        /// <param name="name">The case-sensitive generated value name.</param>
        /// <param name="value">The value of the declared parameter type to use on every execution.</param>
        /// <returns>The scenario for further configuration.</returns>
        /// <exception cref="GeneratedValuesMustBePinnedBeforeExecution">Execution has already begun.</exception>
        public CommandScenario<TCommand> PinGeneratedValue(string name, object value)
        {
            FixtureFor(scenario).Pin(typeof(TCommand), name, value);
            return scenario;
        }

        /// <summary>
        /// Pins a generated parameter for another command, such as a nested command.
        /// </summary>
        /// <remarks>
        /// Specify both type arguments at the call site: PinGeneratedValue&lt;TCommand, TOtherCommand&gt;.
        /// </remarks>
        /// <typeparam name="TOtherCommand">The command declaring the generated parameter.</typeparam>
        /// <param name="name">The case-sensitive generated value name.</param>
        /// <param name="value">The value of the declared parameter type to use on every execution.</param>
        /// <returns>The scenario for further configuration.</returns>
        /// <exception cref="GeneratedValuesMustBePinnedBeforeExecution">Execution has already begun.</exception>
        public CommandScenario<TCommand> PinGeneratedValue<TOtherCommand>(string name, object value)
        {
            FixtureFor(scenario).Pin(typeof(TOtherCommand), name, value);
            return scenario;
        }

        /// <summary>
        /// Allows unpinned parameters to use their normal generation strategy instead of failing the command.
        /// </summary>
        /// <returns>The scenario for further configuration.</returns>
        /// <exception cref="GeneratedValuesMustBePinnedBeforeExecution">Execution has already begun.</exception>
        public CommandScenario<TCommand> AllowGeneration()
        {
            FixtureFor(scenario).AllowGeneration();
            return scenario;
        }
    }

    static ScenarioGeneratedValueFixture FixtureFor<TCommand>(CommandScenario<TCommand> scenario) =>
        (ScenarioGeneratedValueFixture)scenario.Context[GeneratedValuesCommandScenarioExtender.ContextKey];
}
