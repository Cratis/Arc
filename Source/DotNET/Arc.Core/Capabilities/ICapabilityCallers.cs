// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities;

/// <summary>
/// Provides the admitted capability caller for the current request.
/// </summary>
public interface ICapabilityCallers
{
    /// <summary>
    /// Gets the current caller, or null outside a capability call.
    /// </summary>
    CapabilityCaller? Current { get; }
}
