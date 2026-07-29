// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.ProxyGenerator.ControllerBased.for_MethodInfoExtensions;

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
