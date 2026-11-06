// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerPrincipal;

public class when_reading_from_a_request_without_one : Specification
{
    ClaimsPrincipal? _empty;
    ClaimsPrincipal? _wrongType;

    void Establish()
    {
        var request = Substitute.For<IHttpRequestContext>();
        request.Items.Returns(new Dictionary<object, object?>());
        _empty = VerifiedCallerPrincipal.From(request);
        var other = Substitute.For<IHttpRequestContext>();
        other.Items.Returns(new Dictionary<object, object?> { [VerifiedCallerPrincipal.ItemKey] = "not a principal" });
        _wrongType = VerifiedCallerPrincipal.From(other);
    }

    [Fact] void should_return_null_when_absent() => _empty.ShouldBeNull();

    [Fact] void should_return_null_when_the_item_is_not_a_principal() => _wrongType.ShouldBeNull();
}
