// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities;

/// <summary>
/// Endpoint metadata marking one endpoint on the capability port as reachable without a verified caller when
/// <see cref="CapabilitiesOptions.GatedEndpoints"/> is unset and the whole port is gated, such as an OAuth discovery document
/// a client must read before it can authenticate. It exempts only the endpoint carrying it; everything else stays gated.
/// With an explicit <see cref="CapabilitiesOptions.GatedEndpoints"/> selection it has no effect.
/// </summary>
public sealed class CapabilityGateExemption
{
    CapabilityGateExemption()
    {
    }

    /// <summary>
    /// Gets the single instance.
    /// </summary>
    public static CapabilityGateExemption Instance { get; } = new();
}
