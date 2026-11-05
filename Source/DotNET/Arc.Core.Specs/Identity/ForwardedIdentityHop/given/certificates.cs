// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Arc.Identity.ForwardedIdentityHop.given;

public sealed class certificates : IDisposable
{
    readonly RSA _key = RSA.Create(2048);
    readonly List<X509Certificate2> _leaves = [];
    public string Bundle { get; } = Path.Combine(Path.GetTempPath(), $"arc-hop-{Guid.NewGuid():N}.pem");
    public X509Certificate2 Root { get; }

    public certificates()
    {
        var request = new CertificateRequest($"CN=Arc test authority {Guid.NewGuid():N}", _key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
        Root = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-3), DateTimeOffset.UtcNow.AddDays(3));
        File.WriteAllText(Bundle, Root.ExportCertificatePem());
    }

    public X509Certificate2 Leaf(string? dns = "authproxy.internal", bool clientAuth = true, bool expired = false)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=authproxy.internal", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new(clientAuth ? "1.3.6.1.5.5.7.3.2" : "1.3.6.1.5.5.7.3.1") }, true));
        if (dns is not null)
        {
            var names = new SubjectAlternativeNameBuilder();
            names.AddDnsName(dns);
            request.CertificateExtensions.Add(names.Build());
        }
        using var signed = request.Create(Root, DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(expired ? -1 : 1), RandomNumberGenerator.GetBytes(16));
        var result = signed.CopyWithPrivateKey(key);
        _leaves.Add(result);
        return result;
    }

    public void Dispose()
    {
        foreach (var leaf in _leaves) leaf.Dispose();
        Root.Dispose();
        _key.Dispose();
        File.Delete(Bundle);
    }
}
