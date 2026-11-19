// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Availability.for_AvailabilitySubjectProvider;

static class Principals
{
    public static ClaimsPrincipal WithClaims(ClaimsPrincipal principal, params Claim[] claims)
    {
        ((ClaimsIdentity)principal.Identity!).AddClaims(claims);
        return principal;
    }
}
