// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines the scoped testing seam for pinning and recording generated values.
/// </summary>
internal interface IGeneratedValueFixture
{
    /// <summary>
    /// Tries to resolve an immutable fixture pin for the command and name.
    /// </summary>
    /// <param name="commandType">The command being executed.</param>
    /// <param name="name">The generated value name.</param>
    /// <param name="valueType">The declared value type.</param>
    /// <param name="value">The pinned value when found.</param>
    /// <returns>Whether the fixture supplies a value.</returns>
    bool TryGet(Type commandType, string name, Type valueType, out object value);

    /// <summary>
    /// Records a successfully validated generated value.
    /// </summary>
    /// <param name="commandType">The command being executed.</param>
    /// <param name="value">The validated capture.</param>
    void Record(Type commandType, GeneratedValue value);
}
