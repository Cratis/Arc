// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Defines a system that can access the current <see cref="TenantId"/>.
/// </summary>
public interface ITenantIdAccessor
{
    /// <summary>
    /// Gets the current <see cref="TenantId"/>.
    /// </summary>
    TenantId Current { get; }
}