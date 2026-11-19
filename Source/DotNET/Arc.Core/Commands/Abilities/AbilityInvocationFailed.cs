// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Abilities;

/// <summary>
/// Thrown when an ability invoked from a command operation fails. The command fails and compensation follows the
/// commit disposition.
/// </summary>
/// <param name="system">The system.</param>
/// <param name="ability">The ability.</param>
/// <param name="reason">Why it failed.</param>
public class AbilityInvocationFailed(string system, string ability, string? reason)
    : Exception($"Ability '{system}.{ability}' failed{(string.IsNullOrEmpty(reason) ? string.Empty : $": {reason}")}")
{
    /// <summary>Gets the system.</summary>
    public string System { get; } = system;

    /// <summary>Gets the ability.</summary>
    public string Ability { get; } = ability;
}
