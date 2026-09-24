// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_CurrentPrincipalAccessor;

public class when_a_server_scope_is_nested_inside_authorized_work : given.a_current_principal_accessor
{
    ClaimsPrincipal? _nested;
    ClaimsPrincipal? _restored;

    void Establish() => SetupNoHttpRequest();

    void Because()
    {
        using (_accessor.UseAuthorizationPrincipal(_requestUser))
        {
            using (_accessor.BeginScope(_systemUser))
            {
                _nested = _accessor.Current;
            }

            _restored = _accessor.Current;
        }
    }

    [Fact] void should_use_the_explicit_nested_actor() => _nested.ShouldEqual(_systemUser);
    [Fact] void should_restore_the_outer_authorized_actor() => _restored.ShouldEqual(_requestUser);
}
