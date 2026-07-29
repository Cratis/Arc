// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Arc.ProxyGenerator.ControllerBased.for_MethodInfoExtensions;

public class RoleSecuredTypes
{
    [Roles("Librarian")]
    public void SingleRoleFromConstructor() { }

    [Roles("Librarian", "Admin")]
    public void MultipleRolesFromConstructor() { }

    [Authorize(Roles = "Librarian")]
    public void RoleFromNamedArgument() { }

    [Roles("Librarian")]
    [Authorize(Roles = "Librarian")]
    public void RoleFromBothForms() { }
}
