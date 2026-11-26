// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// The exception that is thrown when the grant-all-audiences switch is on in an environment that does not allow it.
/// </summary>
/// <param name="environmentName">The environment the switch was turned on in.</param>
public class EnvironmentSwitchRefusedException(string? environmentName) : Exception(
    $"Granting every declared audience is refused in the '{environmentName ?? "(unnamed)"}' environment. The switch is only allowed in local and test environments.")
{
    /// <summary>
    /// Gets the environment the switch was turned on in.
    /// </summary>
    public string? EnvironmentName { get; } = environmentName;
}
