// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Context;

/// <summary>
/// The exception that is thrown when a required context value is absent. The host refuses the request with this reason.
/// </summary>
/// <param name="name">The name of the required value.</param>
public class RequiredContextValueAbsentException(string name) : Exception($"The required context value '{name}' is absent.")
{
    /// <summary>
    /// Gets the name of the required value.
    /// </summary>
    public string Name { get; } = name;
}
