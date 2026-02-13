// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Defines a system that can resolve the <see cref="TenantId"/> from the current context.
/// </summary>
public interface ITenantIdResolver
{
    /// <summary>
    /// Resolve the <see cref="TenantId"/> from the current context.
    /// </summary>
    /// <returns>The resolved <see cref="TenantId"/> or an empty string if not found.</returns>
    string Resolve();
}
