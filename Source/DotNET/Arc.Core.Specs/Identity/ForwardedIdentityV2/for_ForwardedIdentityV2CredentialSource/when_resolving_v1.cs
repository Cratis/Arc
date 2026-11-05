// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Identity.Callers.given;
using Cratis.Arc.Identity.ForwardedIdentityV2.given;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2CredentialSource;

public class when_resolving_v1 : Specification
{
    CallerResolution _result;

    async Task Because()
    {
        var vector = ForwardedIdentityV2Vectors.Named("credential-kind-browser-session") with { Envelope = "v1.synthetic.signature" };
        var request = VerificationFixture.RawRequest(vector);
        var requests = Substitute.For<IHttpRequestContextAccessor>();
        requests.Current.Returns(request);
        var tenants = Substitute.For<ITenantIdAccessor>();
        tenants.Current.Returns(verified_facts.Tenant);
        var source = VerificationFixture.Source(vector, v2Only: true);
        var resolver = new VerifiedCallerResolver(requests, [source], new(), new(verified_facts.Catalog), verified_facts.Installation, tenants);
        _result = await resolver.GetCurrent();
    }

    [Fact] void should_refuse_v1_through_the_real_resolver_in_v2_only_mode() => _result.Refusal.ShouldEqual(CallerRefusal.SourceRefused);
}
