// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQuerySubscriptionHttpRequestContext;

public class when_selecting_the_authorized_principal : Specification
{
    ClaimsPrincipal _emissionPrincipal;
    ClaimsPrincipal _selected;
    ObservableQuerySubscriptionHttpRequestContext _context;

    void Establish()
    {
        var request = Substitute.For<IHttpRequestContext>();
        request.User.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "default")], "Default")));
        request.Items.Returns(new Dictionary<object, object?>());
        _context = new ObservableQuerySubscriptionHttpRequestContext(
            request,
            Substitute.For<IHttpRequestContext>(),
            new ServiceCollection().BuildServiceProvider(),
            CancellationToken.None);
        _selected = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "selected")], "Special"));
    }

    void Because()
    {
        _context.SelectAuthorizedPrincipal(_selected);
        _selected.AddIdentity(new ClaimsIdentity([new Claim("mutated", "true")]));
        _context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "injected")], "test"));
        _emissionPrincipal = _context.GetPrincipal();
    }

    [Fact] void should_use_the_selected_identity_in_the_emission_snapshot() => _emissionPrincipal.Identity!.Name.ShouldEqual("selected");
    [Fact] void should_use_the_selected_identity_in_the_context_accessor() => _context.User.Identity!.Name.ShouldEqual("selected");
    [Fact] void should_not_retain_later_mutations_of_the_authorized_principal() => _emissionPrincipal.HasClaim("mutated", "true").ShouldBeFalse();
}
