// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Knows which environments may use the grant-all-audiences switch.
/// </summary>
/// <remarks>
/// The switch is allowed only in environments known to be local or test. Production is refused, and so is any
/// environment name that is not recognized: an unknown name is treated as potentially production.
/// </remarks>
public static class AvailabilityEnvironments
{
    static readonly string[] _allowed = ["Development", "Local", "Test", "Testing"];

    /// <summary>
    /// Determines whether the grant-all-audiences switch may be on in the environment.
    /// </summary>
    /// <param name="environmentName">The name of the environment, for example from 'IHostEnvironment.EnvironmentName'.</param>
    /// <returns><see langword="true"/> for a local or test environment; otherwise <see langword="false"/>.</returns>
    public static bool AllowsGrantAll(string? environmentName) =>
        environmentName is not null && _allowed.Contains(environmentName, StringComparer.OrdinalIgnoreCase);
}
