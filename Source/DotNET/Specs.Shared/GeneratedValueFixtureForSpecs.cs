// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>A test-only proxy base for the internal generated-value-fixture seam.</summary>
public class GeneratedValueFixtureForSpecs : IGeneratedValueFixture
{
    /// <inheritdoc/>
    public bool TryGet(Type commandType, string name, Type valueType, out object value) => Resolve(commandType, name, valueType, out value);

    /// <inheritdoc/>
    public void Record(Type commandType, GeneratedValue value) => Capture(commandType, value);

    /// <summary>Supplies the fixture pin configured by a spec.</summary>
    /// <param name="commandType">The command type.</param>
    /// <param name="name">The generated value name.</param>
    /// <param name="valueType">The declared value type.</param>
    /// <param name="value">The configured fixture pin.</param>
    /// <returns>Whether the fixture supplies a value.</returns>
    public virtual bool Resolve(Type commandType, string name, Type valueType, out object value)
    {
        value = null!;
        return false;
    }

    /// <summary>Captures a generated value for assertions.</summary>
    /// <param name="commandType">The command type.</param>
    /// <param name="value">The generated value.</param>
    public virtual void Capture(Type commandType, GeneratedValue value)
    {
    }
}
