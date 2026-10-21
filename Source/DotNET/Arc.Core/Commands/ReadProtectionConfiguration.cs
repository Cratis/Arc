// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
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

    /// <summary>
    /// Determines whether command key lookup requires generated metadata instead of reflection compatibility.
    /// </summary>
    /// <param name="configuration">The host configuration, when available.</param>
    /// <returns>True when explicitly required or dynamic code is unsupported.</returns>
    internal static bool RequireGeneratedMetadata(IConfiguration? configuration) =>
        !RuntimeFeature.IsDynamicCodeSupported ||
        (bool.TryParse(configuration?["ReadProtection:RequireGeneratedMetadata"], out var required) && required);
}
