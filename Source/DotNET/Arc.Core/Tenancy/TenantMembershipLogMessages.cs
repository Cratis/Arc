// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Tenancy;

#pragma warning disable SA1600 // Elements should be documented

internal static partial class TenantMembershipLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Tenancy uses the {Resolver} resolver, which takes the tenant from the request, and no ITenantMembership is registered. Arc does not verify that callers belong to the tenant they select. Register an ITenantMembership, use an authorized tenant claim, or configure a trusted ingress that enforces caller membership and prevents direct backend access before setting Cratis:Arc:Tenancy:IngressValidatesTenant. AuthProxy tenant-existence verification alone does not enforce membership. See https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/tenancy/membership.md")]
    internal static partial void MembershipNotConfigured(this ILogger<TenantMembershipStartupWarning> logger, string resolver);

    [LoggerMessage(LogLevel.Information, "Tenant membership denied by {MembershipType}.")]
    internal static partial void MembershipDenied(this ILogger<TenantMembershipVerification> logger, string membershipType);

    [LoggerMessage(LogLevel.Warning, "Tenant membership check failed for {MembershipType}.")]
    internal static partial void VerificationFailed(this ILogger<TenantMembershipVerification> logger, string membershipType, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Tenant memberships could not be resolved.")]
    internal static partial void ResolutionFailed(this ILogger<TenantMembershipVerification> logger, Exception exception);
}
