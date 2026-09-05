// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_CurrentPrincipalAccessor;

public class when_beginning_a_scope_during_an_http_request : given.a_current_principal_accessor
{
    ClaimsPrincipal? _insideScope;

    void Establish() => SetupHttpRequest();

    void Because()
    {
        using (_accessor.BeginScope(_systemUser))
        {
            _insideScope = _accessor.Current;
        }
    }

    [Fact] void should_ignore_the_override_and_keep_the_request_principal() => _insideScope.ShouldEqual(_requestUser);
}
