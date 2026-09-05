// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Represents a tenant for development tooling.
/// </summary>
/// <param name="Id">The tenant identifier.</param>
/// <param name="Name">The tenant name.</param>
public record Tenant(TenantId Id, TenantName Name);
