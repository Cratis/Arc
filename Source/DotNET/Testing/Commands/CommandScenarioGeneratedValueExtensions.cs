// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        /// Fixes received times and the scenario's UTC wall clock before initialization.
        /// </summary>
        /// <remarks>
        /// Calls to TimeProvider.GetUtcNow() and GetLocalNow() use this instant; timers and elapsed-time measurement remain real.
        /// </remarks>
        /// <param name="instant">The instant used by the scenario's clock.</param>
        /// <returns>The scenario for further configuration.</returns>
        /// <exception cref="GeneratedValuesMustBePinnedBeforeExecution">Execution has already begun.</exception>
        /// <exception cref="ScenarioClockMustBeConfiguredBeforeInitialization">Execute or Validate has already initialized the service provider.</exception>
        public CommandScenario<TCommand> Clock(DateTimeOffset instant)
        {
            var fixture = FixtureFor(scenario);
            fixture.Clock(instant, scenario.IsInitialized);
            scenario.Services.RemoveAll<TimeProvider>();
            scenario.Services.AddSingleton<TimeProvider>(fixture.TimeProvider);

            return scenario;
        }

        /// <summary>
        /// Selects a tenant for executions and validation without changing the caller's tenant.
        /// </summary>
        /// <param name="tenant">The tenant used by this scenario.</param>
        /// <returns>The scenario for further configuration.</returns>
        /// <exception cref="GeneratedValuesMustBePinnedBeforeExecution">Execution has already begun.</exception>
        public CommandScenario<TCommand> Tenant(TenantId tenant)
        {
            FixtureFor(scenario).Tenant(tenant);
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

    /// <summary>
    /// Gets the scenario's immutable generation configuration.
    /// </summary>
    /// <typeparam name="TCommand">The command under test.</typeparam>
    /// <param name="scenario">The scenario holding the fixture.</param>
    /// <returns>The scenario-local fixture.</returns>
    internal static ScenarioGeneratedValueFixture FixtureFor<TCommand>(CommandScenario<TCommand> scenario) =>
        (ScenarioGeneratedValueFixture)scenario.Context[GeneratedValuesCommandScenarioExtender.ContextKey];
}
