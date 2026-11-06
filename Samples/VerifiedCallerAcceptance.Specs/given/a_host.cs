// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace VerifiedCallerAcceptance.Specs.given;

public sealed class a_host : IAsyncLifetime
{
    WebApplication _app = null!;
    public TestSigner Signer { get; } = new();
    public HttpClient Client { get; } = new() { Timeout = TimeSpan.FromSeconds(10) };
    public int CapabilityPort { get; } = FreePort();
    public int DefaultPort { get; } = FreePort();
    public InvocationCounts Counts => _app.Services.GetRequiredService<InvocationCounts>();

    public async ValueTask InitializeAsync()
    {
        _app = Program.Build([], new(CapabilityPort, DefaultPort, Signer.PublicKeyPem, Signer.KeyId));
        await _app.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
        Client.Dispose();
        Signer.Dispose();
    }

    static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
