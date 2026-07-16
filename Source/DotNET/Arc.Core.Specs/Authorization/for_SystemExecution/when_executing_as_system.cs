// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_SystemExecution;

public class when_executing_as_system : given.a_system_execution
{
    void Because() => _systemExecution.AsSystem("Admin");

    [Fact] void should_begin_a_scope_with_an_authenticated_principal() => _currentPrincipalOverride.Received(1).BeginScope(Arg.Is<ClaimsPrincipal>(p => p.Identity!.IsAuthenticated));
    [Fact] void should_begin_a_scope_carrying_the_declared_role() => _currentPrincipalOverride.Received(1).BeginScope(Arg.Is<ClaimsPrincipal>(p => p.IsInRole("Admin")));
    [Fact] void should_not_carry_undeclared_roles() => _currentPrincipalOverride.Received(1).BeginScope(Arg.Is<ClaimsPrincipal>(p => !p.IsInRole("User")));
}
