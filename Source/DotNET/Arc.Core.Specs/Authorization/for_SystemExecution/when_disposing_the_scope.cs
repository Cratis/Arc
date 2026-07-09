// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_SystemExecution;

public class when_disposing_the_scope : given.a_system_execution
{
    ClaimsPrincipal? _currentAfterDispose;

    void Because()
    {
        var scope = _systemExecution.AsSystem("Admin");
        scope.Dispose();
        _currentAfterDispose = _systemExecution.Current;
    }

    [Fact] void should_clear_the_current_principal() => _currentAfterDispose.ShouldBeNull();
}
