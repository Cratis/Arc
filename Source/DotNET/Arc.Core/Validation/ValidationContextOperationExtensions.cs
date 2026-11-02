// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Cratis.Arc.Tenancy;
using FluentValidation;

namespace Cratis.Arc.Validation;

/// <summary>
/// Extensions for reading the current operation's tenant and caller from an <see cref="IValidationContext"/>.
/// </summary>
/// <remarks>
/// The values come from one snapshot per command or query, shared by filters, generated values and child validators.
/// A read outside Arc's command or query pipeline throws <see cref="OperationSnapshotUnavailable"/>, and the operation
/// is rejected as unauthorized. A caller whose tenant membership is denied is rejected the same way.
/// </remarks>
public static class ValidationContextOperationExtensions
{
    /// <summary>
    /// Gets the resolved tenant of the operation being validated.
    /// </summary>
    /// <param name="context">The <see cref="IValidationContext"/> to read from.</param>
    /// <returns>The resolved <see cref="TenantId"/>, or <see cref="TenantId.NotSet"/> when none was resolved.</returns>
    /// <remarks>
    /// The tenant is membership-verified only when an <see cref="ITenantMembership"/> hook is registered; see
    /// the tenant membership documentation. If the caller's claims were mutated in place after the operation began,
    /// the read fails closed with <see cref="TenantMembershipDenied"/>.
    /// </remarks>
    /// <exception cref="OperationSnapshotUnavailable">There is no operation snapshot on the context.</exception>
    /// <exception cref="TenantMembershipDenied">The caller is not a member of the resolved tenant.</exception>
    public static TenantId GetTenant(this IValidationContext context) => SnapshotOf(context).Tenant;

    /// <summary>
    /// Gets the identity that caused the operation being validated.
    /// </summary>
    /// <param name="context">The <see cref="IValidationContext"/> to read from.</param>
    /// <returns>The <see cref="CausingIdentity"/>; <see cref="CausingIdentity.Anonymous"/> for an unauthenticated caller.</returns>
    /// <remarks>
    /// This is Arc's view of the caller. It is not the Chronicle audit identity.
    /// </remarks>
    /// <exception cref="OperationSnapshotUnavailable">There is no operation snapshot on the context.</exception>
    public static CausingIdentity GetCausedBy(this IValidationContext context) => SnapshotOf(context).CausedBy;

    /// <summary>
    /// Gets whether the operation being validated runs as the trusted system actor.
    /// </summary>
    /// <param name="context">The <see cref="IValidationContext"/> to read from.</param>
    /// <returns>True only when the operation's principal is the system principal.</returns>
    /// <exception cref="OperationSnapshotUnavailable">There is no operation snapshot on the context.</exception>
    public static bool IsSystem(this IValidationContext context) => SnapshotOf(context).IsSystem;

    static OperationSnapshot SnapshotOf(IValidationContext context) =>
        context.RootContextData.TryGetValue(OperationSnapshot.ContextKey, out var value) && value is OperationSnapshot { Completed: false } snapshot
            ? snapshot
            : throw new OperationSnapshotUnavailable();
}
