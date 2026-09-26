// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Keeps the authenticated scheme identity and tenant visible throughout authorized command or query execution.
/// </summary>
/// <param name="accessor">The Arc principal accessor.</param>
/// <param name="runtime">The active host's authorization runtime.</param>
/// <param name="tenantIds">The cached tenant for this execution flow.</param>
/// <param name="tenantResolver">Resolves the tenant under the selected identity.</param>
internal class AuthorizationPrincipalScope(
    CurrentPrincipalAccessor accessor,
    IAuthorizationPolicyRuntime runtime,
    TenantIdAccessor tenantIds,
    ITenantIdResolver tenantResolver)
{
    /// <summary>
    /// Begins an execution scope only in a fresh Arc-owned provider, isolating a selected tenant from the outer cache.
    /// </summary>
    /// <param name="principal">The selected principal.</param>
    /// <param name="services">The executing service scope.</param>
    /// <returns>The execution identity scope.</returns>
    /// <exception cref="InvalidAuthorizationConfiguration">The supplied provider is not an Arc-owned clean execution scope.</exception>
    internal IDisposable Begin(ClaimsPrincipal principal, IServiceProvider services)
    {
        AuthorizationExecutionScopes.Bind(services, principal);

        var arcScope = accessor.UseAuthorizationPrincipal(principal, services);
        IDisposable? hostScope = null;
        IDisposable? tenantScope = null;
        try
        {
            hostScope = runtime.BeginPrincipalScope(principal, services);
            if (tenantIds.ExplicitTenant is { } explicitTenant)
            {
                tenantScope = tenantIds.UseAuthorizedTenant(explicitTenant);
            }
            else
            {
                var resolved = tenantResolver.Resolve();
                var selectedTenant = string.IsNullOrEmpty(resolved) ? TenantId.NotSet : new TenantId(resolved);
                tenantScope = tenantIds.UseAuthorizedTenant(selectedTenant);
            }
            return new CombinedScope(arcScope, hostScope, tenantScope);
        }
        catch
        {
            tenantScope?.Dispose();
            hostScope?.Dispose();
            arcScope.Dispose();
            throw;
        }
    }

    sealed class CombinedScope(IDisposable arcScope, IDisposable? hostScope, IDisposable tenantScope) : IDisposable
    {
        public void Dispose()
        {
            try
            {
                tenantScope.Dispose();
            }
            finally
            {
                try
                {
                    hostScope?.Dispose();
                }
                finally
                {
                    arcScope.Dispose();
                }
            }
        }
    }
}
