// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization.given;
using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Queries.for_ObservableQueryCaller;

public class when_comparing_verified_callers : Specification
{
    ObservableQueryCaller _owner;
    ObservableQueryCaller _sameSubject;
    ObservableQueryCaller _anotherSubject;
    ObservableQueryCaller _anotherTenant;
    ObservableQueryCaller _anotherIssuer;
    ObservableQueryCaller _anonymous;
    ObservableQueryCaller _unidentified;
    ObservableQueryCaller _anotherUnidentified;
    ObservableQueryCaller _named;
    ObservableQueryCaller _sameName;
    ObservableQueryCaller _anotherScheme;

    void Because()
    {
        _owner = Capture("owner", "tenant", "issuer");
        _sameSubject = Capture("owner", "tenant", "issuer");
        _anotherSubject = Capture("another", "tenant", "issuer");
        _anotherTenant = Capture("owner", "other-tenant", "issuer");
        _anotherIssuer = Capture("owner", "tenant", "other-issuer");
        _anonymous = CapturePrincipal(new ClaimsPrincipal(new ClaimsIdentity()));
        _unidentified = CapturePrincipal(new ClaimsPrincipal(new ClaimsIdentity([], "test")));
        _anotherUnidentified = CapturePrincipal(new ClaimsPrincipal(new ClaimsIdentity([], "test")));
        _named = CapturePrincipal(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller")], "first-scheme")));
        _sameName = CapturePrincipal(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller")], "first-scheme")));
        _anotherScheme = CapturePrincipal(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller")], "second-scheme")));
    }

    [Fact] void should_recognize_the_same_verified_subject() => _owner.IsSameCallerAs(_sameSubject).ShouldBeTrue();
    [Fact] void should_distinguish_verified_subjects() => _owner.IsSameCallerAs(_anotherSubject).ShouldBeFalse();
    [Fact] void should_distinguish_tenants() => _owner.IsSameCallerAs(_anotherTenant).ShouldBeFalse();
    [Fact] void should_distinguish_issuers() => _owner.IsSameCallerAs(_anotherIssuer).ShouldBeFalse();
    [Fact] void should_distinguish_anonymous_from_authenticated() => _anonymous.IsSameCallerAs(_unidentified).ShouldBeFalse();
    [Fact] void should_not_match_authenticated_callers_without_a_stable_key() => _unidentified.IsSameCallerAs(_anotherUnidentified).ShouldBeFalse();
    [Fact] void should_not_match_an_unidentified_caller_to_itself() => _unidentified.IsSameCallerAs(_unidentified).ShouldBeFalse();
    [Fact] void should_preserve_anonymous_ownership() => _anonymous.IsSameCallerAs(_anonymous).ShouldBeTrue();
    [Fact] void should_preserve_name_only_ownership_within_a_scheme() => _named.IsSameCallerAs(_sameName).ShouldBeTrue();
    [Fact] void should_distinguish_authentication_schemes_for_the_same_name() => _named.IsSameCallerAs(_anotherScheme).ShouldBeFalse();
    [Fact] void should_not_match_anonymous_to_a_verified_caller() => _anonymous.IsSameCallerAs(_owner).ShouldBeFalse();

    static ObservableQueryCaller Capture(string subject, string tenant, string issuer)
    {
        var principal = VerifiedCallerPrincipal.Create(verified_callers.Person(subject) with { Tenant = new(tenant) });
        ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim(VerifiedCallerIdentity.IssuerClaim, issuer));
        var context = Substitute.For<IHttpRequestContext>();
        context.Items.Returns(new Dictionary<object, object?> { [VerifiedCallerPrincipal.ItemKey] = principal });
        context.User.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "host-user")], "host")));
        return ObservableQueryCaller.CapturedFrom(context);
    }

    static ObservableQueryCaller CapturePrincipal(ClaimsPrincipal principal)
    {
        var context = Substitute.For<IHttpRequestContext>();
        context.Items.Returns(new Dictionary<object, object?>());
        context.User.Returns(principal);
        return ObservableQueryCaller.CapturedFrom(context);
    }
}
