// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_ForwardedIdentityHop;

public class when_exercising_the_real_kestrel_hop : Specification
{
    readonly Dictionary<string, HttpStatusCode?> _outcomes = [];
    readonly List<X509Certificate2> _certificates = [];
    string _bundle;
    string _serverFingerprint;

    async Task Because()
    {
        using var keyA = RSA.Create(2048);
        using var keyB = RSA.Create(2048);
        var rootA = Root(keyA);
        var rootB = Root(keyB);
        var clientA = Leaf(rootA, "authproxy.internal", true);
        var clientB = Leaf(rootB, "authproxy.internal", true);
        var server = Leaf(rootA, "localhost", false);
        _serverFingerprint = server.GetCertHashString(HashAlgorithmName.SHA256);
        _bundle = Path.Combine(Path.GetTempPath(), $"arc-kestrel-hop-{Guid.NewGuid():N}.pem");
        await File.WriteAllTextAsync(_bundle, rootA.ExportCertificatePem());
        var options = ForwardedIdentityForSpecs.Options();
        options.ForwardedIdentity.ClientCertificate = new()
        {
            Required = true,
            TrustedCertificateAuthoritiesPath = _bundle,
            AllowedDnsNames = ["authproxy.internal"],
            ReloadInterval = TimeSpan.FromSeconds(5)
        };
        var monitor = Substitute.For<IOptionsMonitor<ArcOptions>>();
        monitor.CurrentValue.Returns(options);
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        var clock = new Clock();
        builder.Services.AddSingleton<TimeProvider>(clock);
        builder.Services.AddCertificateForwarding(forwarding =>
        {
            forwarding.CertificateHeader = "X-Forwarded-Client-Cert";
            forwarding.HeaderConverter = _ => clientA;
        });
        builder.Services.AddSingleton(monitor);
        builder.Services.AddSingleton(Options.Create(options));
        builder.Services.AddSingleton<ForwardedIdentityClientCertificateTrustStore>();
        ListenOptions noCertificateListener = null;
        builder.WebHost.ConfigureKestrel(kestrel => ConfigureListeners(kestrel, server, listen => noCertificateListener = listen));
        await using var app = builder.Build();
        app.Services.GetRequiredService<ForwardedIdentityClientCertificateTrustStore>().ValidateConfiguration();
        app.UseCertificateForwarding();
        app.Run(async context =>
        {
            // Force the actual forwarding middleware to materialize its spoofed TLS feature.
            _ = await context.Connection.GetClientCertificateAsync();
            var schemeOptions = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
            schemeOptions.Get(Arg.Any<string>()).Returns(new AuthenticationSchemeOptions());
            var handler = new MicrosoftIDentityPlatformAuthHandler(schemeOptions, monitor, NullLoggerFactory.Instance, UrlEncoder.Default);
            await handler.InitializeAsync(new AuthenticationScheme(MicrosoftIDentityPlatformAuthHandler.SchemeName, null, typeof(MicrosoftIDentityPlatformAuthHandler)), context);
            var result = await handler.AuthenticateAsync();
            context.Response.StatusCode = result.Succeeded ? 200 : 401;
        });
        await app.StartAsync();
        try
        {
            var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses;
            var noCertificate = $"https://127.0.0.1:{((IPEndPoint)noCertificateListener.EndPoint).Port}";
            var tls = addresses.Single(address => address.StartsWith("https:", StringComparison.Ordinal) && address != noCertificate);
            var clear = addresses.Single(address => address.StartsWith("http:", StringComparison.Ordinal));
            using var admitted = Client(clientA);
            using var otherCa = Client(clientB);
            using var unrelated = Client(Leaf(rootA, "unrelated.internal", true));
            using var missing = Client(null);
            using var expired = Client(Leaf(rootA, "authproxy.internal", true, true));
            using var wrongEku = Client(Leaf(rootA, "authproxy.internal", false));
            _outcomes["trusted"] = await Send(admitted, tls);
            _outcomes["listener-not-requesting-certificates"] = await Send(admitted, noCertificate);
            _outcomes["unrelated"] = await Send(unrelated, tls);
            _outcomes["wrong-ca"] = await Send(otherCa, tls);
            _outcomes["missing"] = await Send(missing, tls);
            _outcomes["expired"] = await Send(expired, tls);
            _outcomes["wrong-eku"] = await Send(wrongEku, tls);
            _outcomes["http-spoof"] = await Send(missing, clear, true);
            _outcomes["tls-spoof"] = await Send(missing, tls, true);
            await File.WriteAllTextAsync(_bundle, rootA.ExportCertificatePem() + "\n" + rootB.ExportCertificatePem());
            clock.Advance();
            _outcomes["overlap-a"] = await Send(admitted, tls);
            _outcomes["overlap-b"] = await Send(otherCa, tls);
            await File.WriteAllTextAsync(_bundle, rootB.ExportCertificatePem());
            clock.Advance();
            _outcomes["retired-a"] = await Send(admitted, tls);
            _outcomes["current-b"] = await Send(otherCa, tls);
            await File.WriteAllTextAsync(_bundle, "malformed");
            clock.Advance();
            _outcomes["bad-bundle"] = await Send(otherCa, tls);
            await File.WriteAllTextAsync(_bundle, rootB.ExportCertificatePem());
            clock.Advance();
            _outcomes["recovered"] = await Send(otherCa, tls);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    void Destroy()
    {
        foreach (var certificate in _certificates) certificate.Dispose();
        if (_bundle is not null) File.Delete(_bundle);
    }

    [Fact] void should_accept_the_real_trusted_peer() => _outcomes["trusted"].ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_refuse_even_a_trusted_caller_when_the_listener_does_not_request_certificates() => _outcomes["listener-not-requesting-certificates"].ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_refuse_all_hostile_tls_peers() => new[] { "unrelated", "wrong-ca", "missing", "expired", "wrong-eku" }.All(Refused).ShouldBeTrue();
    [Fact] void should_ignore_forwarded_transport_headers_on_http_and_tls() => new[] { "http-spoof", "tls-spoof" }.All(Refused).ShouldBeTrue();
    [Fact] void should_support_live_ca_rotation_and_recovery() => new[] { "overlap-a", "overlap-b", "current-b", "bad-bundle", "recovered" }.All(name => _outcomes[name] == HttpStatusCode.OK).ShouldBeTrue();
    [Fact] void should_refuse_the_removed_authority() => Refused("retired-a").ShouldBeTrue();

    sealed class Clock : TimeProvider
    {
        DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        internal void Advance() => _now += TimeSpan.FromSeconds(5);
    }

    bool Refused(string name) => _outcomes[name] is null or HttpStatusCode.Unauthorized;

    HttpClient Client(X509Certificate2? certificate)
    {
        var handler = new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, remote, _, _) => remote?.GetCertHashString(HashAlgorithmName.SHA256) == _serverFingerprint,
                ClientCertificates = certificate is null ? [] : [certificate]
            }
        };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
    }

    static void ConfigureListeners(KestrelServerOptions kestrel, X509Certificate2 server, Action<ListenOptions> onNoCertificate)
    {
        // The host explicitly delegates transport certificate validation to Arc's admission policy.
        kestrel.ConfigureHttpsDefaults(https =>
        {
            https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
            https.AllowAnyClientCertificate();
        });
        kestrel.Listen(IPAddress.Loopback, 0);
        kestrel.Listen(IPAddress.Loopback, 0, listen => listen.UseHttps(server));
        kestrel.Listen(IPAddress.Loopback, 0, listen =>
        {
            onNoCertificate(listen);
            listen.UseHttps(server, https => https.ClientCertificateMode = ClientCertificateMode.NoCertificate);
        });
    }

    static async Task<HttpStatusCode?> Send(HttpClient client, string address, bool spoof = false)
    {
        var headers = ForwardedIdentityForSpecs.Headers();
        headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(headers, timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(), nonce: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_'));
        using var request = new HttpRequestMessage(HttpMethod.Post, address + "/base/a%2fb?x=1");
        foreach (var pair in headers) request.Headers.TryAddWithoutValidation(pair.Key, pair.Value);
        if (spoof)
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");
            request.Headers.TryAddWithoutValidation("X-Forwarded-Client-Cert", "synthetic-forwarded-certificate");
        }
        try
        {
            using var response = await client.SendAsync(request);
            return response.StatusCode;
        }
        catch (HttpRequestException)
        {
            // TLS refusal may precede HTTP. Positive controls must still return 200.
            return null;
        }
    }

    X509Certificate2 Root(RSA key)
    {
        var request = new CertificateRequest($"CN=Arc Kestrel authority {Guid.NewGuid():N}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
        var root = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-3), DateTimeOffset.UtcNow.AddDays(3));
        _certificates.Add(root);
        return root;
    }

    X509Certificate2 Leaf(X509Certificate2 root, string dns, bool client, bool expired = false)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={dns}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new(client ? "1.3.6.1.5.5.7.3.2" : "1.3.6.1.5.5.7.3.1") }, true));
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName(dns);
        request.CertificateExtensions.Add(san.Build());
        using var signed = request.Create(root, DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(expired ? -1 : 1), RandomNumberGenerator.GetBytes(16));
        var certificate = signed.CopyWithPrivateKey(key);
        _certificates.Add(certificate);
        return certificate;
    }
}
