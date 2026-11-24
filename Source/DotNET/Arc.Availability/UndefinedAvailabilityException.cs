// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// The exception that is thrown when a gate uses an availability block that is not defined. A model error, not a denial.
/// </summary>
/// <param name="name">The name of the missing block.</param>
public class UndefinedAvailabilityException(string name) : Exception($"The availability '{name}' is not defined.")
{
    /// <summary>
    /// Gets the name of the missing block.
    /// </summary>
    public string Name { get; } = name;
}
