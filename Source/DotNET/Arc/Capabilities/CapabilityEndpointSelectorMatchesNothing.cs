// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities;

/// <summary>
/// The exception thrown when a configured capability endpoint selector selects no route endpoints.
/// </summary>
public class CapabilityEndpointSelectorMatchesNothing()
    : Exception("Capability endpoint selection matches no registered route endpoints. Configure a matching route pattern or endpoint metadata type, or leave GatedEndpoints unset to gate the whole port.");
