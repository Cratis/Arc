// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.ProxyGenerator.ControllerBased.for_MethodInfoExtensions;

/// <summary>
/// Holds the shapes roles can be declared in, for reading them back.
/// </summary>
/// <remarks>
/// Both <c>Cratis.Arc.Authorization</c> and <c>Microsoft.AspNetCore.Authorization</c> declare an
/// <c>Authorize</c> attribute, so the ASP.NET Core one is written out in full where the named-argument form is needed.
/// </remarks>
public class RoleSecuredTypes
{
    [Roles("Librarian")]
    public void SingleRoleFromConstructor() { }

    [Roles("Librarian", "Admin")]
    public void MultipleRolesFromConstructor() { }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Librarian")]
    public void RoleFromNamedArgument() { }

    [Roles("Librarian")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Librarian")]
    public void RoleFromBothForms() { }
}
