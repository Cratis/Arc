// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers.given;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.given;

public class a_resolver : Specification
{
    internal IHttpRequestContext _request;
    internal ICanReadForwardedIdentityRequest _raw;
    internal IHttpRequestContextAccessor _requests;
    internal ITenantIdAccessor _tenants;
    internal IVerifiedCredentialSource _source;
    internal readonly List<IVerifiedCredentialSource> _sources = [];
    internal VerifiedCallerResolver _resolver;
    internal CallerResolution _result;

    void Establish()
    {
        _request = NewRequest();
        _raw = (ICanReadForwardedIdentityRequest)_request;
        _requests = Substitute.For<IHttpRequestContextAccessor>();
        _requests.Current.Returns(_request);
        _tenants = Substitute.For<ITenantIdAccessor>();
        _tenants.Current.Returns(verified_facts.Tenant);
        _source = Substitute.For<VerifiedCredentialSourceForSpecs>();
        _source.Shape.Returns(PresentedCredentialShape.OAuthBearer);
        _source.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns(CredentialVerification.Verified(verified_facts.Person()));
        _sources.Add(_source);
        _resolver = new(_requests, _sources, new(), new(verified_facts.Catalog), verified_facts.Installation, _tenants);
    }

    internal static IHttpRequestContext NewRequest()
    {
        var request = (IHttpRequestContext)Substitute.For<CredentialHeaderPreservationForSpecs, IHttpRequestContext, ICanReadForwardedIdentityRequest>();
        request.Items.Returns(new Dictionary<object, object?>());
        request.Query.Returns(new Dictionary<string, string>());
        ((ICanPreserveCallerCredentialHeaders)request).PreservesDuplicateCredentialHeaders.Returns(true);
        var raw = (ICanReadForwardedIdentityRequest)request;
        raw.RawTarget.Returns("/mcp");
        raw.HeaderNames.Returns(["Authorization"]);
        raw.GetHeaderValues(Arg.Any<string>()).Returns([]);
        raw.GetHeaderValues("Authorization").Returns(["Bearer synthetic-fixture"]);
        return request;
    }
}
