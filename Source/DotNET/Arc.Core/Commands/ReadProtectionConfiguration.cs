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
    const string RootSection = "ReadProtection";

    /// <summary>
    /// Reads a read-protection setting. The namespaced key (<c>Cratis:Arc:ReadProtection:{name}</c>) wins when present;
    /// the original root key (<c>ReadProtection:{name}</c>) keeps working when it is absent.
    /// </summary>
    /// <param name="configuration">The host configuration, when available.</param>
    /// <param name="name">The setting name within the read-protection section.</param>
    /// <returns>The configured value, or null when neither key is set.</returns>
    internal static string? Value(IConfiguration? configuration, string name) =>
        configuration?[ConfigurationPath.Combine([.. HostBuilderExtensions.DefaultSectionPaths, RootSection, name])]
        ?? configuration?[ConfigurationPath.Combine(RootSection, name)];

    /// <summary>
    /// Gets the configured ceiling. Missing or invalid values leave declarations unchanged.
    /// </summary>
    /// <param name="configuration">The host configuration, when available.</param>
    /// <returns>The maximum allowed rollout mode.</returns>
    internal static ReadProtectionMode MaximumMode(IConfiguration? configuration) =>
        Enum.TryParse<ReadProtectionMode>(Value(configuration, "MaximumMode"), true, out var maximum) && Enum.IsDefined(maximum)
            ? maximum
            : ReadProtectionMode.On;

    /// <summary>
    /// Determines whether command key lookup requires generated metadata instead of reflection compatibility.
    /// </summary>
    /// <param name="configuration">The host configuration, when available.</param>
    /// <returns>True unless explicitly set to false on a runtime that supports dynamic code. Missing or invalid values require metadata.</returns>
    internal static bool RequireGeneratedMetadata(IConfiguration? configuration) =>
        !RuntimeFeature.IsDynamicCodeSupported ||
        !(bool.TryParse(Value(configuration, "RequireGeneratedMetadata"), out var required) && !required);
}
