// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization.given;
using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Queries.for_ObservableQuerySubscriptionHttpRequestContext;

public class when_capturing_an_authorization_selected_principal : Specification
{
    ClaimsPrincipal _anonymous;
    ClaimsPrincipal _scheme;
    ClaimsPrincipal _selected;
    ClaimsPrincipal _verified;

    void Because()
    {
        var request = Substitute.For<IHttpRequestContext>();
        request.Items.Returns(new Dictionary<object, object?>
        {
            [VerifiedCallerPrincipal.ItemKey] = VerifiedCallerPrincipal.Create(verified_callers.Person("admitted", "admin"))
        });
        request.User.Returns(VerifiedCallerPrincipal.Create(verified_callers.Person("host", "admin")));
        _anonymous = ObservableQuerySubscriptionHttpRequestContext.CaptureEmissionPrincipal(request, new ClaimsPrincipal(new ClaimsIdentity()));
        _selected = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "selected")], "selected-scheme"));
        _scheme = ObservableQuerySubscriptionHttpRequestContext.CaptureEmissionPrincipal(request, _selected);
        var verified = VerifiedCallerPrincipal.Create(verified_callers.Person("selected-verified", "read"));
        _verified = ObservableQuerySubscriptionHttpRequestContext.CaptureEmissionPrincipal(request, verified);
        ((ClaimsIdentity)verified.Identity!).AddClaim(new Claim(VerifiedCallerIdentity.ScopeClaim, "admin"));
    }

    [Fact] void should_keep_an_explicit_anonymous_selection() => _anonymous.Identity!.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_not_restore_the_verified_subject_for_an_anonymous_selection() => _anonymous.FindFirst("sub").ShouldBeNull();
    [Fact] void should_keep_the_selected_scheme() => _scheme.Identity!.AuthenticationType.ShouldEqual("selected-scheme");
    [Fact] void should_keep_the_selected_subject() => _scheme.FindFirst("sub")!.Value.ShouldEqual("selected");
    [Fact] void should_preserve_legacy_selected_principal_handling() => ReferenceEquals(_scheme, _selected).ShouldBeTrue();
    [Fact] void should_keep_a_verified_authorization_selected_subject() => _verified.FindFirst("sub")!.Value.ShouldEqual("selected-verified");
    [Fact] void should_isolate_a_verified_selected_principal_from_later_mutation() => _verified.HasClaim(VerifiedCallerIdentity.ScopeClaim, "admin").ShouldBeFalse();
}
