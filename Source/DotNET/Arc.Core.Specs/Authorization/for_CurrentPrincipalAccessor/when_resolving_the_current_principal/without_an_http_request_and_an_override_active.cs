// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_CurrentPrincipalAccessor.when_resolving_the_current_principal;

public class without_an_http_request_and_an_override_active : given.a_current_principal_accessor
{
    ClaimsPrincipal? _result;

    void Establish() => SetupNoHttpRequest();

    void Because()
    {
        using (_accessor.BeginScope(_systemUser))
        {
            _result = _accessor.Current;
        }
    }

    [Fact] void should_be_the_override_principal() => _result.ShouldEqual(_systemUser);
}
