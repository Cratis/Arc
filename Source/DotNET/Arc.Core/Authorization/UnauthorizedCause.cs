// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Identifies exceptions that reject an operation as unauthorized.
/// </summary>
internal static class UnauthorizedCause
{
    /// <summary>
    /// Checks whether an exception or one of its nested causes is a tenant membership denial or an unavailable operation snapshot.
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns>True when the operation must be rejected as unauthorized.</returns>
    internal static bool IsCauseOf(Exception? exception) =>
        TenantMembershipDenied.IsCauseOf(exception) || OperationSnapshotUnavailable.IsCauseOf(exception);
}
