// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities;

/// <summary>
/// The exception thrown for duplicate or overlapping capability endpoint selector rules.
/// </summary>
public class CapabilityEndpointSelectorIsAmbiguous()
    : Exception("Capability endpoint selection contains duplicate or case-colliding route patterns, or duplicate or assignable metadata types. Remove the overlapping rules.");
