// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities;

/// <summary>
/// The exception thrown when capabilities are enabled without a port for their group.
/// </summary>
/// <param name="group">The capability endpoint group.</param>
public class CapabilityEndpointGroupHasNoPort(string group)
    : Exception($"Capability endpoint group '{group}' has no port. Assign one with ArcOptions.EndpointGroups.Assign(\"{group}\", <port>).");
