// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_SystemPrincipal;

public class when_creating_without_roles : Specification
{
    ClaimsPrincipal _result;

    void Because() => _result = SystemPrincipal.WithRoles();

    [Fact] void should_be_authenticated() => _result.Identity!.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_not_be_in_any_role() => _result.IsInRole("Admin").ShouldBeFalse();
}
