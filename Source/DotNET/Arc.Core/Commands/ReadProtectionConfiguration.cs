// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.Commands;

/// <summary>
/// Resolves the operational ceiling without changing compile-time declarations.
/// </summary>
internal static class ReadProtectionConfiguration
{
    /// <summary>
    /// Gets the configured ceiling. Missing or invalid values leave declarations unchanged.
    /// </summary>
    /// <param name="configuration">The host configuration, when available.</param>
    /// <returns>The maximum allowed rollout mode.</returns>
    internal static ReadProtectionMode MaximumMode(IConfiguration? configuration) =>
        Enum.TryParse<ReadProtectionMode>(configuration?["ReadProtection:MaximumMode"], true, out var maximum) && Enum.IsDefined(maximum)
            ? maximum
            : ReadProtectionMode.On;
}
