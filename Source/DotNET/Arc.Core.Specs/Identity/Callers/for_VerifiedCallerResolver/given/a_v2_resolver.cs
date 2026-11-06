// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Arc.Http;
using Cratis.Arc.Identity.ForwardedIdentityV2;
using Cratis.Arc.Identity.ForwardedIdentityV2.given;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.given;

public class a_v2_resolver : Specification
{
    static readonly Lazy<(Identity.for_ForwardedIdentityHop.given.certificates Authority, X509Certificate2 First, X509Certificate2 Second)> _certificates = new(() =>
    {
        var authority = new Identity.for_ForwardedIdentityHop.given.certificates();
        return (authority, authority.Leaf("proxy.test"), authority.Leaf("proxy.test"));
    });

    internal IHttpRequestContext _request;
    internal ICanReadClientCertificate _transport;
    internal ArcOptions _options;
    internal PersonDirectory _persons;
    internal VerifiedCallerResolver _resolver;
    internal CallerResolution _first;
    internal CallerResolution _result;

    void Establish()
    {
        var vector = VerificationFixture.DirectBrowser();
        _request = VerificationFixture.RawRequest(vector);
        _transport = (ICanReadClientCertificate)_request;
        _transport.TransportClientCertificate.Returns(_ => new X509Certificate2(_certificates.Value.First));
        _options = _request.RequestServices.GetRequiredService<IOptions<ArcOptions>>().Value;
        _options.ForwardedIdentity.ClientCertificate.TrustedCertificateAuthoritiesPath = _certificates.Value.Authority.Bundle;
        var requests = Substitute.For<IHttpRequestContextAccessor>();
        requests.Current.Returns(_request);
        var tenants = Substitute.For<ITenantIdAccessor>();
        tenants.Current.Returns(new TenantId("tenant-test"));
        _persons = new();
        var source = new ForwardedIdentityV2CredentialSource(requests, VerificationFixture.Settings(vector.Inputs), VerificationFixture.Verifier(vector), new("resource", "test"), _persons, NullLogger<ForwardedIdentityV2CredentialSource>.Instance);
        var catalog = new CallerResourceCatalog(new Dictionary<string, string> { ["resource"] = vector.Inputs.Aud });
        _resolver = new(requests, [source], new(), new(catalog), new("resource", "test"), tenants);
    }

    internal void ChangeCertificate() => _transport.TransportClientCertificate.Returns(_ => new X509Certificate2(_certificates.Value.Second));

    void Destroy() => ((IDisposable)_request.RequestServices).Dispose();
}
