// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization.given;
using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Queries.for_ObservableQuerySubscriptionHttpRequestContext;

public class when_capturing_a_verified_principal : Specification
{
    ObservableQuerySubscriptionHttpRequestContext _snapshot;
    ClaimsPrincipal _captured;
    ClaimsPrincipal _legacy;
    ClaimsPrincipal _legacyCaptured;

    void Because()
    {
        var admitted = VerifiedCallerPrincipal.Create(verified_callers.Person("admitted", "read"));
        var request = Substitute.For<IHttpRequestContext>();
        var items = new Dictionary<object, object?> { [VerifiedCallerPrincipal.ItemKey] = admitted };
        request.Items.Returns(items);
        request.Headers.Returns(new Dictionary<string, string>());
        request.Query.Returns(new Dictionary<string, string>());
        request.User.Returns(VerifiedCallerPrincipal.Create(verified_callers.Person("host", "admin")));
        _snapshot = new(request, request, Substitute.For<IServiceProvider>(), CancellationToken.None);
        _captured = ObservableQuerySubscriptionHttpRequestContext.CaptureEmissionPrincipal(request);
        items[VerifiedCallerPrincipal.ItemKey] = request.User;
        ((ClaimsIdentity)admitted.Identity!).AddClaim(new Claim(VerifiedCallerIdentity.ScopeClaim, "admin"));
        _legacy = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "legacy")], "host"));
        var legacyRequest = Substitute.For<IHttpRequestContext>();
        legacyRequest.Items.Returns(new Dictionary<object, object?>());
        legacyRequest.User.Returns(_legacy);
        _legacyCaptured = ObservableQuerySubscriptionHttpRequestContext.CaptureEmissionPrincipal(legacyRequest);
    }

    [Fact] void should_capture_the_admitted_subject() => _snapshot.User.FindFirst(VerifiedCallerIdentity.SubjectClaim)!.Value.ShouldEqual("admitted");
    [Fact] void should_preserve_the_verified_identity_type() => (_snapshot.User.Identity is VerifiedCallerIdentity).ShouldBeTrue();
    [Fact] void should_freeze_the_direct_stream_subject() => _captured.FindFirst(VerifiedCallerIdentity.SubjectClaim)!.Value.ShouldEqual("admitted");
    [Fact] void should_not_gain_privileges_from_mutation() => _captured.HasClaim(VerifiedCallerIdentity.ScopeClaim, "admin").ShouldBeFalse();
    [Fact] void should_not_gain_privileges_in_the_subscription() => _snapshot.User.HasClaim(VerifiedCallerIdentity.ScopeClaim, "admin").ShouldBeFalse();
    [Fact] void should_keep_legacy_principal_handling_unchanged() => ReferenceEquals(_legacyCaptured, _legacy).ShouldBeTrue();
}
