// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQuerySubscriptionHttpRequestContext;

public class when_getting_the_frozen_principal : Specification
{
    ObservableQuerySubscriptionHttpRequestContext _context;
    ClaimsPrincipal _first;
    ClaimsPrincipal _second;

    void Establish()
    {
        var requestContext = Substitute.For<IHttpRequestContext>();
        requestContext.User.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "original")], "test")));
        requestContext.Items.Returns(new Dictionary<object, object?>());
        var transportContext = Substitute.For<IHttpRequestContext>();
        _context = new ObservableQuerySubscriptionHttpRequestContext(
            requestContext,
            transportContext,
            new ServiceCollection().BuildServiceProvider(),
            CancellationToken.None);
    }

    void Because()
    {
        _first = _context.GetPrincipal();
        _first.AddIdentity(new ClaimsIdentity([new Claim("mutated", "true")]));
        _second = _context.GetPrincipal();
    }

    [Fact] void should_return_independent_principals() => ReferenceEquals(_first, _second).ShouldBeFalse();
    [Fact] void should_not_expose_mutations_to_later_reads() => _second.HasClaim("mutated", "true").ShouldBeFalse();
    [Fact] void should_preserve_the_frozen_claims() => _second.Identity!.Name.ShouldEqual("original");
}
