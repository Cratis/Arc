// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Certificates = Cratis.Arc.Identity.for_ForwardedIdentityHop.given.certificates;

namespace VerifiedCallerAcceptance.Specs.given;

public sealed class a_host : IAsyncLifetime
{
    readonly Certificates _certificates = new();
    WebApplication _app = null!;
    public TestSigner Signer { get; } = new();
    public HttpClient Client { get; private set; } = null!;
    public HttpClient NoCertificateClient { get; private set; } = null!;
    public int CapabilityPort { get; } = FreePort();
    public int DefaultPort { get; } = FreePort();
    public InvocationCounts Counts => _app.Services.GetRequiredService<InvocationCounts>();

    public async ValueTask InitializeAsync()
    {
        var server = _certificates.Leaf("localhost", clientAuth: false);
        Client = CreateClient(server, _certificates.Leaf());
        NoCertificateClient = CreateClient(server, null);
        _app = Program.Build([], new(CapabilityPort, DefaultPort, Signer.PublicKeyPem, server, _certificates.Bundle, "authproxy.internal", Signer.KeyId));
        _app.Services.GetRequiredService<IOptions<AuthenticationOptions>>().Value.DefaultAuthenticateScheme = "ProductHeader";
        _app.Services.GetRequiredService<IAuthenticationSchemeProvider>().AddScheme(new("ProductHeader", null, typeof(ProductHeader)));
        await _app.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
        Client.Dispose();
        NoCertificateClient.Dispose();
        Signer.Dispose();
        _certificates.Dispose();
    }

    static HttpClient CreateClient(X509Certificate2 server, X509Certificate2? peer) => new(new SocketsHttpHandler
    {
        SslOptions = new SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = (_, remote, _, _) => remote?.GetCertHashString(HashAlgorithmName.SHA256) == server.GetCertHashString(HashAlgorithmName.SHA256),
            ClientCertificates = peer is null ? [] : [peer]
        }
    })
    { Timeout = TimeSpan.FromSeconds(10) };

    static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
