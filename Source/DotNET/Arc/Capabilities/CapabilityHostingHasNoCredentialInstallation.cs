// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities;

/// <summary>
/// The exception that is thrown when capability hosting is turned on without a usable credential installation.
/// </summary>
/// <param name="missing">What the installation lacks.</param>
public class CapabilityHostingHasNoCredentialInstallation(string missing)
    : Exception($"Capability hosting cannot admit callers: {missing}. Capability hosting requires a verified-caller credential installation, which is not yet available as a public API.");
