// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Authorization.for_CurrentPrincipalAccessor;

public class when_a_request_carries_a_verified_principal : given.a_current_principal_accessor
{
    ClaimsPrincipal _verified;
    ClaimsPrincipal _result;

    void Establish()
    {
        SetupHttpRequest();
        _verified = Authorization.given.verified_callers.PrincipalFor(Authorization.given.verified_callers.Person("person-1"));
        _httpRequestContext.Items.Returns(new Dictionary<object, object?> { [VerifiedCallerPrincipal.ItemKey] = _verified });
    }

    void Because() => _result = _accessor.Current!;

    [Fact] void should_return_the_verified_principal_not_the_request_user() => _result.ShouldEqual(_verified);

    [Fact] void should_not_be_the_request_user() => _result.ShouldNotEqual(_requestUser);
}
