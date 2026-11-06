// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Http;
using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Identity.for_ForwardedIdentityHop;

public class when_certificate_forwarding_spoofs_the_transport : for_ForwardedIdentityAuthentication.given.a_signed_handler
{
    AuthenticateResult _result;
    X509Certificate2? _transportCertificate;

    async Task Because()
    {
        _options.ForwardedIdentity.ClientCertificate.Required = true;
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=forwarded-not-transport", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1));
        _context.Request.Scheme = "https";
        _context.Request.Headers["X-Forwarded-Proto"] = "https";
        _context.Request.Headers["X-Forwarded-Client-Cert"] = "synthetic-forwarded-certificate";
        _context.Connection.ClientCertificate = certificate;
        _transportCertificate = ((ICanReadClientCertificate)new AspNetCoreHttpRequestContext(_context)).TransportClientCertificate;
        _result = await Authenticate();
    }

    [Fact] void should_not_accept_a_forwarded_tls_feature() => _transportCertificate.ShouldBeNull();
    [Fact] void should_refuse_signed_forwarding_without_the_real_tls_peer() => _result.None.ShouldBeTrue();
}
