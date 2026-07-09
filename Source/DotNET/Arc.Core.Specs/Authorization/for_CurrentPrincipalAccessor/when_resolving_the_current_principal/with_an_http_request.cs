// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_CurrentPrincipalAccessor.when_resolving_the_current_principal;

public class with_an_http_request : given.a_current_principal_accessor
{
    ClaimsPrincipal? _result;

    void Establish() => SetupHttpRequest();

    void Because() => _result = _accessor.Current;

    [Fact] void should_be_the_request_principal() => _result.ShouldEqual(_requestUser);
}
