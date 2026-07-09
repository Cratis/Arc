// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_CurrentPrincipalAccessor;

public class when_nesting_override_scopes : given.a_current_principal_accessor
{
    ClaimsPrincipal? _afterInnerDisposed;

    void Establish() => SetupNoHttpRequest();

    void Because()
    {
        using (_accessor.BeginScope(_systemUser))
        {
            using (_accessor.BeginScope(new ClaimsPrincipal(new ClaimsIdentity("inner"))))
            {
            }

            _afterInnerDisposed = _accessor.Current;
        }
    }

    [Fact] void should_restore_the_outer_principal() => _afterInnerDisposed.ShouldEqual(_systemUser);
}
