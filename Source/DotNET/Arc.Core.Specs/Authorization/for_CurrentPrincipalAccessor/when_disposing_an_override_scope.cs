// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_CurrentPrincipalAccessor;

public class when_disposing_an_override_scope : given.a_current_principal_accessor
{
    ClaimsPrincipal? _afterDispose;

    void Establish() => SetupNoHttpRequest();

    void Because()
    {
        var scope = _accessor.BeginScope(_systemUser);
        scope.Dispose();
        _afterDispose = _accessor.Current;
    }

    [Fact] void should_restore_the_previous_principal() => _afterDispose.ShouldBeNull();
}
