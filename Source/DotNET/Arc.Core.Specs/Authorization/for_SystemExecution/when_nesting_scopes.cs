// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_SystemExecution;

public class when_nesting_scopes : given.a_system_execution
{
    ClaimsPrincipal? _outerAfterInnerDisposed;

    void Because()
    {
        using (_systemExecution.AsSystem("Outer"))
        {
            using (_systemExecution.AsSystem("Inner"))
            {
            }

            _outerAfterInnerDisposed = _systemExecution.Current;
        }
    }

    [Fact] void should_restore_the_outer_principal() => _outerAfterInnerDisposed.ShouldNotBeNull();
    [Fact] void should_restore_the_outer_role() => _outerAfterInnerDisposed!.IsInRole("Outer").ShouldBeTrue();
    [Fact] void should_not_retain_the_inner_role() => _outerAfterInnerDisposed!.IsInRole("Inner").ShouldBeFalse();
}
