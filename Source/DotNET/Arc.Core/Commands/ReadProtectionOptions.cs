// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents the read-protection settings, under <c>Cratis:Arc:ReadProtection</c>.
/// </summary>
/// <remarks>
/// This is a mirror of configuration. The effective values are resolved from <c>IConfiguration</c> by the read-protection
/// runtime (the namespaced key wins over the original root <c>ReadProtection</c> section), so set them in configuration;
/// assigning them in code in <c>AddCratis(options =&gt; ...)</c> is not consulted. A value left <see langword="null"/> is unset.
/// The namespaced key is always <c>Cratis:Arc:ReadProtection</c>, regardless of a custom configuration section path.
/// </remarks>
public class ReadProtectionOptions
{
    /// <summary>
    /// Gets or sets the operational ceiling for the rollout mode, for instance <c>On</c>, <c>Measure</c> or <c>Off</c>.
    /// </summary>
    public string? MaximumMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether command key lookup requires generated metadata instead of reflection compatibility.
    /// </summary>
    public string? RequireGeneratedMetadata { get; set; }

    /// <summary>
    /// Gets or sets the startup admission check setting, for instance <c>Off</c>.
    /// </summary>
    public string? StartupAdmissionCheck { get; set; }

    /// <summary>
    /// Gets or sets the Measure-mode tail timeout in milliseconds.
    /// </summary>
    public string? MeasureTailTimeout { get; set; }

    /// <summary>
    /// Gets or sets the Measure-mode scan timeout in milliseconds.
    /// </summary>
    public string? MeasureScanTimeout { get; set; }

    /// <summary>
    /// Gets or sets the Measure-mode scan limit.
    /// </summary>
    public string? MeasureScanLimit { get; set; }

    /// <summary>
    /// Gets or sets the Measure-mode sample rate, from 0 to 1.
    /// </summary>
    public string? MeasureSampleRate { get; set; }
}
