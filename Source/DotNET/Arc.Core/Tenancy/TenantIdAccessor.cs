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
    static readonly AsyncLocal<ExplicitTenantFrame?> _explicit = new();

    /// <inheritdoc/>
    public TenantId Current
    {
        get
        {
            if (_explicit.Value is { } frame)
            {
                return frame.Tenant;
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
    internal TenantId? ExplicitTenant => _explicit.Value?.Tenant;

    /// <inheritdoc/>
    public IDisposable Begin(TenantId tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        if (string.IsNullOrWhiteSpace(tenant.Value))
        {
            throw new ArgumentException("A tenant ID must not be empty.", nameof(tenant));
        }

        var frame = new ExplicitTenantFrame(tenant, new object(), _explicit.Value);
        _explicit.Value = frame;

        return new ExplicitTenantScope(frame.Identity);
    }

    /// <summary>
    /// Temporarily binds tenant resolution to the identity selected for an authorized operation.
    /// </summary>
    /// <param name="tenant">The selected tenant.</param>
    /// <returns>A scope restoring the previously cached tenant.</returns>
    internal IDisposable UseAuthorizedTenant(TenantId tenant) => UseTenant(_explicit.Value?.Tenant ?? tenant);

    static TenantScope UseTenant(TenantId tenant)
    {
        var previous = _current.Value;
        _current.Value = tenant;
        return new TenantScope(previous);
    }

    /// <summary>
    /// Immutable so removing a scope in one async flow cannot mutate frames inherited by another.
    /// </summary>
    /// <param name="Tenant">The tenant selected by this frame.</param>
    /// <param name="Identity">The identity of this scope across frame copies.</param>
    /// <param name="Previous">The previously selected frame.</param>
    sealed record ExplicitTenantFrame(TenantId Tenant, object Identity, ExplicitTenantFrame? Previous);

    sealed class ExplicitTenantScope(object identity) : IDisposable
    {
        public void Dispose() => _explicit.Value = Remove(_explicit.Value, identity);

        static ExplicitTenantFrame? Remove(ExplicitTenantFrame? frame, object identity)
        {
            if (frame is null || ReferenceEquals(frame.Identity, identity))
            {
                return frame?.Previous;
            }

            var previous = Remove(frame.Previous, identity);

            return ReferenceEquals(previous, frame.Previous) ? frame : frame with { Previous = previous };
        }
    }

    sealed class TenantScope(TenantId? previous) : IDisposable
    {
        public void Dispose() => _current.Value = previous;
    }
}
