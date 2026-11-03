// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.given;

/// <summary>A principal accessor returning one principal without allocating.</summary>
/// <param name="principal">The principal.</param>
public sealed class FixedPrincipalAccessor(ClaimsPrincipal principal) : ICurrentPrincipalAccessor
{
    /// <inheritdoc/>
    public ClaimsPrincipal? Current => principal;
}
