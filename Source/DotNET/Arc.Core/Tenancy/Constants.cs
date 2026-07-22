// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Holds constants related to tenant id.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Gets the default header name for the tenant id.
    /// </summary>
    public const string DefaultTenantIdHeader = "x-cratis-tenant-id";

    /// <summary>
    /// Gets the item key for the tenant id.
    /// </summary>
    public const string TenantIdItemKey = "TenantId";
}
