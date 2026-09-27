// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointsExtensions.given;

public class normalizing_claims : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal) =>
        Task.FromResult(new ClaimsPrincipal(new ClaimsIdentity(principal.Claims, "Normalized")));
}
