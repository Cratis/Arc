// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization.given;
using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerPrincipal;

public class when_reading_from_a_request_with_one : Specification
{
    ClaimsPrincipal _principal;
    ClaimsPrincipal? _result;

    void Establish()
    {
        _principal = verified_callers.PrincipalFor(verified_callers.Person("person-1"));
        var request = Substitute.For<IHttpRequestContext>();
        request.Items.Returns(new Dictionary<object, object?> { [VerifiedCallerPrincipal.ItemKey] = _principal });
        _result = VerifiedCallerPrincipal.From(request);
    }

    [Fact] void should_return_the_principal() => _result.ShouldEqual(_principal);
}
