// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Audiences;

/// <summary>
/// The level at which an audience was granted.
/// </summary>
public enum AudienceLevel
{
    /// <summary>
    /// Granted to a tenant; every user in the tenant holds it.
    /// </summary>
    Tenant = 0,

    /// <summary>
    /// Granted to one user.
    /// </summary>
    User = 1,

    /// <summary>
    /// Granted to the system caller, such as a background process or an integration.
    /// </summary>
    System = 2
}
