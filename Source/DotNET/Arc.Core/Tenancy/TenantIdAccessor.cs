// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Represents the current tenant accessor and explicit <see cref="ITenantScope"/>.
/// </summary>
/// <param name="tenantIdResolver">The <see cref="ITenantIdResolver"/> to use for resolving tenant IDs.</param>
[Singleton]
public class TenantIdAccessor(ITenantIdResolver tenantIdResolver) : ITenantIdAccessor, ITenantScope
{
    static readonly AsyncLocal<TenantId?> _current = new();
    static readonly AsyncLocal<TenantId?> _explicit = new();

    /// <inheritdoc/>
    public TenantId Current
    {
        get
        {
            if (_explicit.Value is not null)
            {
                return _explicit.Value;
            }

            if (_current.Value is not null)
            {
                return _current.Value;
            }

            var tenantId = tenantIdResolver.Resolve();
            var result = string.IsNullOrEmpty(tenantId) ? TenantId.NotSet : new TenantId(tenantId);
            _current.Value = result;
            return result;
        }
    }

    /// <summary>
    /// Gets the tenant already cached on this execution flow without causing it to resolve.
    /// </summary>
    internal TenantId? Cached => _current.Value;

    /// <summary>
    /// Gets the explicitly selected tenant, if any, without consulting the resolver.
    /// </summary>
    internal TenantId? ExplicitTenant => _explicit.Value;

    /// <inheritdoc/>
    public IDisposable Begin(TenantId tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        if (string.IsNullOrWhiteSpace(tenant.Value))
        {
            throw new ArgumentException("A tenant ID must not be empty.", nameof(tenant));
        }

        var previous = _explicit.Value;
        _explicit.Value = tenant;
        return new ExplicitTenantScope(previous);
    }

    /// <summary>
    /// Temporarily binds tenant resolution to the identity selected for an authorized operation.
    /// </summary>
    /// <param name="tenant">The selected tenant.</param>
    /// <returns>A scope restoring the previously cached tenant.</returns>
    internal IDisposable UseAuthorizedTenant(TenantId tenant) => UseTenant(_explicit.Value ?? tenant);

    static TenantScope UseTenant(TenantId tenant)
    {
        var previous = _current.Value;
        _current.Value = tenant;
        return new TenantScope(previous);
    }

    sealed class ExplicitTenantScope(TenantId? previous) : IDisposable
    {
        public void Dispose() => _explicit.Value = previous;
    }

    sealed class TenantScope(TenantId? previous) : IDisposable
    {
        public void Dispose() => _current.Value = previous;
    }
}
