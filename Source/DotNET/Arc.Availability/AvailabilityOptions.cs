// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Arc.Availability;

/// <summary>
/// Options for the availability engine.
/// </summary>
[IgnoreConvention]
public class AvailabilityOptions
{
    /// <summary>
    /// Gets or sets the name of the environment the host runs in.
    /// </summary>
    public string? EnvironmentName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether every declared audience is granted to every request. Off by default.
    /// </summary>
    /// <remarks>
    /// This exists so a developer or a test run sees the whole application. It is refused outside a local or
    /// test environment, see <see cref="AvailabilityEnvironments"/>.
    /// </remarks>
    public bool GrantAllDeclaredAudiences { get; set; }

    /// <summary>
    /// Verifies the options are allowed in the environment.
    /// </summary>
    /// <exception cref="EnvironmentSwitchRefusedException">The switch is on in an environment that does not allow it.</exception>
    public void Validate()
    {
        if (GrantAllDeclaredAudiences && !AvailabilityEnvironments.AllowsGrantAll(EnvironmentName))
        {
            throw new EnvironmentSwitchRefusedException(EnvironmentName);
        }
    }
}
