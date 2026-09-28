// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Scheme authentication prepared before identity-bound command scopes, with policies evaluated later against the command context.
/// </summary>
/// <param name="Target">The declared target.</param>
/// <param name="Declaration">The effective requirements.</param>
/// <param name="OriginalPrincipal">The caller before scheme authentication.</param>
/// <param name="SelectedPrincipal">The authenticated scheme principal or synthetic guest.</param>
/// <param name="Resolution">The resolved policies used for the later verdict.</param>
internal sealed record PreparedAuthorization(
    MemberInfo Target,
    AuthorizationDeclaration Declaration,
    ClaimsPrincipal? OriginalPrincipal,
    ClaimsPrincipal? SelectedPrincipal,
    IAuthorizationPolicyResolution Resolution)
{
    /// <summary>
    /// Gets whether explicit scheme authentication selected a different principal.
    /// </summary>
    internal bool PrincipalChanged => SelectedPrincipal?.Identity?.IsAuthenticated == true &&
        !AuthorizationPrincipalIdentity.Same(OriginalPrincipal, SelectedPrincipal);

    /// <summary>
    /// Gets whether every policy explicitly opts into evaluating unauthenticated callers.
    /// </summary>
    internal bool EvaluatesAnonymous => Resolution is IAnonymousPolicyResolution { EvaluatesAnonymous: true };
}
