// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// Holds immutable execution pins and validated captures for one scenario.
/// </summary>
internal sealed class ScenarioGeneratedValueFixture : IGeneratedValueFixture
{
    readonly Dictionary<(Type Command, string Name), object> _pins = [];
    bool _allowGeneration;
    bool _frozen;
    TenantId? _tenant;

    /// <summary>
    /// Gets the scenario-local clock.
    /// </summary>
    internal ScenarioTimeProvider TimeProvider { get; } = new();

    /// <summary>
    /// Gets captures across the scenario's command executions.
    /// </summary>
    internal ScenarioGeneratedValues Values { get; } = new();

    /// <inheritdoc/>
    public bool TryGet(Type commandType, string name, Type valueType, out object value)
    {
        Freeze();
        if (_pins.TryGetValue((commandType, name), out value!))
        {
            if (!valueType.IsInstanceOfType(value))
            {
                throw new PinnedGeneratedValueHasWrongType(commandType, name, valueType, value?.GetType());
            }

            return true;
        }

        if (!_allowGeneration)
        {
            throw new GeneratedValueNotPinned(commandType, name);
        }

        return false;
    }

    /// <inheritdoc/>
    public void Record(Type commandType, GeneratedValue value) => Values.Add(value);

    /// <summary>
    /// Reads an explicitly configured pin without generating a value or freezing configuration.
    /// </summary>
    /// <param name="commandType">The command declaring the parameter.</param>
    /// <param name="name">The generated value name.</param>
    /// <param name="value">The configured pin, when present.</param>
    /// <returns>Whether the scenario has a pin for this command and name.</returns>
    internal bool TryGetPin(Type commandType, string name, out object value) =>
        _pins.TryGetValue((commandType, name), out value!);

    /// <summary>
    /// Configures a named pin for a command before execution.
    /// </summary>
    /// <param name="commandType">The command declaring the parameter.</param>
    /// <param name="name">The generated value name.</param>
    /// <param name="value">The pin to supply.</param>
    /// <exception cref="GeneratedValuesMustBePinnedBeforeExecution">Execution has already begun.</exception>
    internal void Pin(Type commandType, string name, object value)
    {
        EnsureConfigurable();
        _pins[(commandType, name)] = value;
    }

    /// <summary>
    /// Allows unpinned values to use normal generation.
    /// </summary>
    /// <exception cref="GeneratedValuesMustBePinnedBeforeExecution">Execution has already begun.</exception>
    internal void AllowGeneration()
    {
        EnsureConfigurable();
        _allowGeneration = true;
    }

    /// <summary>
    /// Sets the scenario's fixed clock before its service provider is initialized.
    /// </summary>
    /// <param name="instant">The captured instant for every operation.</param>
    /// <param name="initialized">Whether the scenario has already built its service provider.</param>
    /// <exception cref="GeneratedValuesMustBePinnedBeforeExecution">Execution has already begun.</exception>
    /// <exception cref="ScenarioClockMustBeConfiguredBeforeInitialization">The service provider has already been built.</exception>
    internal void Clock(DateTimeOffset instant, bool initialized)
    {
        EnsureConfigurable();
        if (initialized)
        {
            throw new ScenarioClockMustBeConfiguredBeforeInitialization();
        }

        TimeProvider.Instant = instant;
    }

    /// <summary>
    /// Sets the scenario's tenant before execution.
    /// </summary>
    /// <param name="tenant">The tenant for every operation.</param>
    /// <exception cref="GeneratedValuesMustBePinnedBeforeExecution">Execution has already begun.</exception>
    internal void Tenant(TenantId tenant)
    {
        EnsureConfigurable();
        _tenant = tenant;
    }

    /// <summary>
    /// Scopes the configured tenant to the scenario's pipeline call.
    /// </summary>
    /// <param name="services">The scenario's service provider.</param>
    /// <returns>A scope restoring the prior tenant, or null if no tenant is configured.</returns>
    internal IDisposable? BeginTenant(IServiceProvider services) => _tenant is null ? null : services.GetRequiredService<ITenantScope>().Begin(_tenant);

    /// <summary>
    /// Makes all pins and generation policy immutable for execution.
    /// </summary>
    internal void Freeze() => _frozen = true;

    void EnsureConfigurable()
    {
        if (_frozen)
        {
            throw new GeneratedValuesMustBePinnedBeforeExecution();
        }
    }
}
