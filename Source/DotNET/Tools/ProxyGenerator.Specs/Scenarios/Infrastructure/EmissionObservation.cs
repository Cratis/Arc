// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// A subscription emission's selected principal and tenant captured by a real guard.
/// </summary>
/// <param name="Principal">The selected caller name.</param>
/// <param name="Tenant">The ambient tenant during emission.</param>
/// <param name="ScopedTenant">The tenant bound to the per-subscription service.</param>
/// <param name="AmbientPrincipal">The Arc ambient principal.</param>
/// <param name="NativePrincipal">The native HTTP principal, when a live request exists.</param>
/// <param name="NativeTenant">The native HTTP provider's tenant, when a live request exists.</param>
/// <param name="NativePrincipalBeforeAwait">The native principal before asynchronous emission guard suspension.</param>
/// <param name="NativeTenantBeforeAwait">The native provider's tenant before suspension.</param>
public record EmissionObservation(
    string? Principal,
    string Tenant,
    string ScopedTenant,
    string? AmbientPrincipal = null,
    string? NativePrincipal = null,
    string? NativeTenant = null,
    string? NativePrincipalBeforeAwait = null,
    string? NativeTenantBeforeAwait = null);
