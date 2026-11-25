// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Context;

/// <summary>
/// The exception that is thrown when a built-in context name is declared as a provided value.
/// </summary>
/// <param name="name">The reserved name.</param>
public class ReservedContextValueNameException(string name) : Exception($"The context name '{name}' is built in and cannot be provided.")
{
    /// <summary>
    /// Gets the reserved name.
    /// </summary>
    public string Name { get; } = name;
}
