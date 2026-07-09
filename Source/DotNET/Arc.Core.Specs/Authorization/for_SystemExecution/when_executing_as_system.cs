// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_SystemExecution;

public class when_executing_as_system : given.a_system_execution
{
    ClaimsPrincipal _current;

    void Because()
    {
        using (_systemExecution.AsSystem("Admin"))
        {
            _current = _systemExecution.Current!;
        }
    }

    [Fact] void should_have_a_current_principal() => _current.ShouldNotBeNull();
    [Fact] void should_be_authenticated() => _current.Identity!.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_be_in_the_declared_role() => _current.IsInRole("Admin").ShouldBeTrue();
    [Fact] void should_not_be_in_an_undeclared_role() => _current.IsInRole("User").ShouldBeFalse();
}
