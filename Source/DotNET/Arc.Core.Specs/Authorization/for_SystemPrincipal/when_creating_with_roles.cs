// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_SystemPrincipal;

public class when_creating_with_roles : Specification
{
    ClaimsPrincipal _result;

    void Because() => _result = SystemPrincipal.WithRoles("Admin", "Manager");

    [Fact] void should_be_authenticated() => _result.Identity!.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_be_in_the_first_declared_role() => _result.IsInRole("Admin").ShouldBeTrue();
    [Fact] void should_be_in_the_second_declared_role() => _result.IsInRole("Manager").ShouldBeTrue();
    [Fact] void should_not_be_in_an_undeclared_role() => _result.IsInRole("Other").ShouldBeFalse();
}
