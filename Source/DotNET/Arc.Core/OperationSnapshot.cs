// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Identity;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc;

/// <summary>
/// Owns the tenant and caller of one command or query for the validators that run inside it.
/// </summary>
/// <remarks>
/// A snapshot is always new and never inherited: a nested, independently started operation begins its own, and the
/// enclosing one is current again once it completes. Beginning captures the ambient values without resolving anything.
/// The tenant and the principal are resolved lazily, once, on first read, from the captured state and never from the
/// ambient principal, so a tenant or principal switch inside a validator cannot change what it reads.
/// <para>
/// The snapshot fingerprints the ambient principal when it begins. If that principal's claims are mutated in place
/// before the first read, every read fails closed with <see cref="TenantMembershipDenied"/>, which rejects the operation
/// as unauthorized. A principal selected later by authorization is not fingerprinted at begin. A custom <see cref="ITenantIdAccessor"/> that is
/// not a <see cref="TenantIdAccessor"/> gives <see cref="TenantProvenance.Unverified"/> provenance and its own
/// <see cref="ITenantIdAccessor.Current"/> semantics.
/// </para>
/// </remarks>
internal sealed class OperationSnapshot : IDisposable
{
    /// <summary>
    /// The key the snapshot is carried under in a validation context's root context data.
    /// </summary>
    internal const string ContextKey = "Cratis.Arc.OperationSnapshot";

    static readonly AsyncLocal<OperationSnapshot?> _current = new();
    readonly OperationSnapshot? _previous;
    readonly Lazy<ClaimsPrincipal?> _principal;
    readonly Lazy<CausingIdentity> _causedBy;
    readonly Lazy<(TenantId Tenant, TenantProvenance Provenance)> _tenant;
    volatile bool _membershipDenied;

    OperationSnapshot(
        OperationSnapshot? previous,
        Func<PrincipalSnapshot?> authorizedPrincipal,
        ClaimsPrincipal? principalAtBegin,
        PrincipalSnapshot capturedPrincipal,
        TenantId? seed,
        TenantProvenance seedProvenance,
        TenantIdAccessor? accessor,
        ITenantIdAccessor? genericAccessor,
        ITenantIdResolver? resolver,
        bool strictMembership)
    {
        _previous = previous;
        StrictMembership = strictMembership;
        _principal = new(() => SelectPrincipal(authorizedPrincipal, principalAtBegin, capturedPrincipal), LazyThreadSafetyMode.ExecutionAndPublication);
        _causedBy = new(() => CausingIdentity.From(_principal.Value), LazyThreadSafetyMode.ExecutionAndPublication);
        _tenant = new(
            () => ResolveTenant(seed, seedProvenance, accessor, genericAccessor, resolver),
            LazyThreadSafetyMode.ExecutionAndPublication);
        _current.Value = this;
    }

    /// <summary>
    /// Gets the snapshot of the operation currently validating, if any.
    /// </summary>
    internal static OperationSnapshot? Current
    {
        get
        {
            // A completed frame captured by a continuation that outlives its operation must not fall back to an
            // enclosing frame, which belongs to another operation.
            var snapshot = _current.Value;
            return snapshot?.Completed == true ? null : snapshot;
        }
    }

    /// <summary>
    /// Gets the principal the operation runs as.
    /// </summary>
    internal ClaimsPrincipal? Principal => _principal.Value;

    /// <summary>
    /// Gets the identity that caused the operation.
    /// </summary>
    internal CausingIdentity CausedBy => _causedBy.Value;

    /// <summary>
    /// Gets a value indicating whether the operation runs as the system principal.
    /// </summary>
    internal bool IsSystem => CausedBy.IsSystem;

    /// <summary>
    /// Gets the resolved tenant of the operation.
    /// </summary>
    /// <exception cref="TenantMembershipDenied">The caller is not a member of the resolved tenant. Every read throws.</exception>
    internal TenantId Tenant => _tenant.Value.Tenant;

    /// <summary>
    /// Gets how much is known about where <see cref="Tenant"/> came from.
    /// </summary>
    internal TenantProvenance TenantProvenance => _tenant.Value.Provenance;

    /// <summary>
    /// Gets a value indicating whether reading the tenant was denied by the membership checks.
    /// </summary>
    internal bool MembershipDenied => _membershipDenied;

    /// <summary>
    /// Gets a value indicating whether a membership denial thrown by any validator rejects the operation.
    /// </summary>
    internal bool StrictMembership { get; }

    /// <summary>
    /// Gets a value indicating whether this snapshot has completed.
    /// </summary>
    internal bool Completed { get; private set; }

    /// <summary>
    /// Begins a new snapshot, never inheriting an enclosing one.
    /// </summary>
    /// <param name="services">The services of the operation.</param>
    /// <param name="authorizedPrincipal">Gets the identity of the principal the authorization filters chose, as captured when it was selected, read on first use; when it returns null the principal ambient at begin is used.</param>
    /// <returns>The new, current snapshot. Dispose it when the operation completes.</returns>
    internal static OperationSnapshot Begin(IServiceProvider services, Func<PrincipalSnapshot?> authorizedPrincipal)
    {
        var principalAtBegin = services.GetService<ICurrentPrincipalAccessor>()?.Current;
        var capturedPrincipal = AuthorizationPrincipalIdentity.Capture(principalAtBegin);

        var genericAccessor = services.GetService<ITenantIdAccessor>();
        var accessor = services.GetService<TenantIdAccessor>() ?? genericAccessor as TenantIdAccessor;
        TenantId? seed = null;
        var provenance = TenantProvenance.NotSet;
        if (accessor?.ExplicitTenant is { } explicitTenant)
        {
            seed = explicitTenant;
            provenance = TenantProvenance.Explicit;
        }
        else if (accessor?.Cached is { } cached)
        {
            seed = cached;
            provenance = TenantProvenance.Unverified;
        }

        var strict = services.GetService<IOptions<ArcOptions>>()?.Value.Tenancy.StrictValidatorMembership == true;
        return new(
            _current.Value,
            authorizedPrincipal,
            principalAtBegin,
            capturedPrincipal,
            seed,
            provenance,
            accessor,
            genericAccessor,
            services.GetService<ITenantIdResolver>(),
            strict);
    }

    /// <inheritdoc/>
#pragma warning disable SA1202 // The disposal must follow the properties it completes; members are otherwise ordered by access.
    public void Dispose()
#pragma warning restore SA1202
    {
        if (Completed)
        {
            return;
        }

        Completed = true;
        _current.Value = _previous;
    }

    ClaimsPrincipal? SelectPrincipal(Func<PrincipalSnapshot?> authorizedPrincipal, ClaimsPrincipal? principalAtBegin, PrincipalSnapshot capturedPrincipal)
    {
        var authorized = authorizedPrincipal();
        var expected = authorized ?? capturedPrincipal;
        var selected = authorized is null ? principalAtBegin : authorized.Reference;

        // A principal whose claims were mutated in place after the operation began is no longer the caller the operation
        // started as. Reading fails closed, for every read, and the pipeline rejects the operation as unauthorized.
        if (!AuthorizationPrincipalIdentity.Same(expected, selected))
        {
            _membershipDenied = true;
            throw new TenantMembershipDenied();
        }

        return selected;
    }

    (TenantId Tenant, TenantProvenance Provenance) ResolveTenant(
        TenantId? seed,
        TenantProvenance seedProvenance,
        TenantIdAccessor? accessor,
        ITenantIdAccessor? genericAccessor,
        ITenantIdResolver? resolver)
    {
        try
        {
            _ = Principal;
        }
        catch (TenantMembershipDenied)
        {
            _membershipDenied = true;
            throw;
        }

        if (seed is not null)
        {
            return (seed, seedProvenance);
        }

        try
        {
            if (accessor is not null && resolver is not null)
            {
                var tenant = accessor.ResolveVerified(resolver, Principal, out var verified);
                return (tenant, tenant == TenantId.NotSet ? TenantProvenance.NotSet : verified ? TenantProvenance.Verified : TenantProvenance.Unverified);
            }

            var current = genericAccessor?.Current ?? TenantId.NotSet;
            return (current, current == TenantId.NotSet ? TenantProvenance.NotSet : TenantProvenance.Unverified);
        }
        catch (TenantMembershipDenied)
        {
            _membershipDenied = true;
            throw;
        }
    }
}
