// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace VerifiedCallerAcceptance;

/// <summary>
/// Enriches a known product editor without granting scopes to the verified credential.
/// </summary>
public sealed class ProductRoles : IClaimsTransformation
{
    /// <inheritdoc/>
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.FindFirst("sub")?.Value == VerifiedCallerAcceptanceSettings.Editor && !principal.IsInRole("editor"))
        {
            principal.AddIdentity(new ClaimsIdentity([new Claim("sub", VerifiedCallerAcceptanceSettings.Editor), new Claim(ClaimTypes.Role, "editor")], "ProductRoles"));
        }
        return Task.FromResult(principal);
    }
}
