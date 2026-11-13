// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OAuthPatCallerAcceptance.Specs.given;

public sealed class Certificates : IDisposable
{
    readonly ECDsa _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    readonly X509Certificate2 _root;
    readonly string _directory = Path.GetFullPath(Path.Combine(".ai-work", "3829", "runtime", Guid.NewGuid().ToString("N")));
    readonly List<X509Certificate2> _leaves = [];

    public Certificates()
    {
        var request = new CertificateRequest("CN=Synthetic OAuth PAT Acceptance CA", _key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
        _root = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Bundle, _root.ExportCertificatePem());
    }

    public string Bundle => Path.Combine(_directory, "ca.pem");

    public X509Certificate2 Leaf(bool client)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest(client ? "CN=proxy.example.test" : "CN=localhost", key, HashAlgorithmName.SHA256);
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName(client ? "proxy.example.test" : "localhost");
        if (!client) san.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(san.Build());
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new(client ? "1.3.6.1.5.5.7.3.2" : "1.3.6.1.5.5.7.3.1") }, true));
        using var certificate = request.Create(_root, DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1), RandomNumberGenerator.GetBytes(16));
        var leaf = certificate.CopyWithPrivateKey(key);
        _leaves.Add(leaf);
        return leaf;
    }

    public void Dispose()
    {
        foreach (var certificate in _leaves) certificate.Dispose();
        _root.Dispose();
        _key.Dispose();
        Directory.Delete(_directory, true);
    }
}
