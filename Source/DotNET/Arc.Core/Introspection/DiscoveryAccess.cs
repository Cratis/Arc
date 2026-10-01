// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection;

/// <summary>
/// Represents how the discovery endpoints are exposed.
/// </summary>
internal enum DiscoveryAccess
{
    /// <summary>
    /// Any caller can reach the endpoints.
    /// </summary>
    Anonymous = 0,

    /// <summary>
    /// Only authenticated callers, holding one of the configured roles if any, can reach the endpoints.
    /// </summary>
    Authenticated = 1,

    /// <summary>
    /// The endpoints are not mapped, because they require authentication and the host cannot authenticate callers.
    /// </summary>
    Unavailable = 2
}
