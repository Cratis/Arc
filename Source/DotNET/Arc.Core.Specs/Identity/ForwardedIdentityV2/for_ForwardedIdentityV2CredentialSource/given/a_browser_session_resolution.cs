// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Identity.ForwardedIdentityV2.given;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2CredentialSource.given;

public class a_browser_session_resolution : Specification
{
    internal static async Task<CallerResolution> Resolve(Vector vector, TenantId tenant, PersonDirectory persons)
    {
        var requests = Substitute.For<IHttpRequestContextAccessor>();
        var request = VerificationFixture.RawRequest(vector);
        requests.Current.Returns(request);
        var tenants = Substitute.For<ITenantIdAccessor>();
        tenants.Current.Returns(tenant);
        var catalog = new CallerResourceCatalog(new Dictionary<string, string> { ["resource"] = vector.Inputs.Aud });
        var resolver = new VerifiedCallerResolver(requests, [VerificationFixture.Source(vector, persons: persons)], new(), new(catalog), new("resource", "test"), tenants);
        return await resolver.GetCurrent();
    }
}
