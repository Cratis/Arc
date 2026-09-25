// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Defines an explicit tenant boundary for the current asynchronous execution flow.
/// </summary>
public interface ITenantScope
{
    /// <summary>
    /// Selects a tenant until the returned scope is disposed, including during an HTTP request.
    /// </summary>
    /// <param name="tenant">The tenant to select.</param>
    /// <returns>A scope that restores the previously selected tenant on disposal.</returns>
    /// <exception cref="ArgumentNullException">The tenant is null.</exception>
    /// <exception cref="ArgumentException">The tenant ID is empty or whitespace.</exception>
    /// <remarks>
    /// A selected tenant takes precedence over the configured resolver, but does not authorize access to that tenant.
    /// Resolve tenant-scoped dependencies in a DI scope created after beginning this tenant scope.
    /// </remarks>
    IDisposable Begin(TenantId tenant);
}
