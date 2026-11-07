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
    /// Gets the allowed principal kinds, defaulting to people and delegated actors.
    /// </summary>
    public ISet<CapabilityPrincipalKind> AllowedPrincipalKinds { get; } = new HashSet<CapabilityPrincipalKind>
    {
        CapabilityPrincipalKind.Person,
        CapabilityPrincipalKind.DelegatedActor
    };
}
