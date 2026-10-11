// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// Holds immutable execution pins and validated captures for one scenario.
/// </summary>
internal sealed class ScenarioGeneratedValueFixture : IGeneratedValueFixture
{
    readonly Dictionary<(Type Command, string Name), object> _pins = [];
    bool _allowGeneration;
    bool _frozen;

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
