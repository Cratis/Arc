// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities;

/// <summary>
/// Configures the endpoint group and principal kinds admitted to capabilities.
/// </summary>
public class CapabilitiesOptions
{
    /// <summary>
    /// Gets or sets the endpoint group whose ports require a verified caller.
    /// </summary>
    public string EndpointGroup { get; set; } = "capabilities";

    /// <summary>
    /// Gets or sets the endpoints that require a verified caller on the capability port.
    /// When unset, every request on that port is gated. An explicitly empty selector is invalid.
    /// Unknown, non-route, or missing endpoints remain gated. Selection is frozen at startup.
    /// </summary>
    public CapabilityEndpointSelector? GatedEndpoints { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the gate runs after endpoint routing even when <see cref="GatedEndpoints"/> is unset.
    /// Every request on the capability port is still gated; routing first lets the gate answer in the protocol of the endpoint it refuses,
    /// such as an MCP refusal. A protocol package that serves through the gate sets this; an explicit <see cref="GatedEndpoints"/> selection implies it.
    /// </summary>
    public bool GateAfterRouting { get; set; }

    /// <summary>
    /// Gets the allowed principal kinds, defaulting to people and delegated actors.
    /// </summary>
    public ISet<CapabilityPrincipalKind> AllowedPrincipalKinds { get; } = new HashSet<CapabilityPrincipalKind>
    {
        CapabilityPrincipalKind.Person,
        CapabilityPrincipalKind.DelegatedActor
    };
}
