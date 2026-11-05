// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using System.Text;
using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Callers.HttpListener.given;

public class a_listener : Specification
{
    internal CredentialPresentation _result;
    internal string[] _observedAuthorization;
    internal bool _preservesDuplicates;

    internal async Task Inspect(string headers)
    {
        var host = OperatingSystem.IsWindows() ? "localhost" : "127.0.0.1";
        using var listener = StartListener(host, out var port);
        using var client = new TcpClient();
        await client.ConnectAsync(host, port);
        var wire = $"POST /mcp HTTP/1.1\r\nHost: {host}:{port}\r\nContent-Length: 0\r\n{headers}\r\n";
        await client.GetStream().WriteAsync(Encoding.ASCII.GetBytes(wire));
        var received = await listener.GetContextAsync().WaitAsync(TimeSpan.FromSeconds(10));
        await using var services = new ServiceCollection().BuildServiceProvider();
        try
        {
            _observedAuthorization = received.Request.Headers.GetValues("Authorization") ?? [];
            var context = new HttpListenerRequestContext(received, services);
            _preservesDuplicates = ((ICanPreserveCallerCredentialHeaders)context).PreservesDuplicateCredentialHeaders;
            _result = new CredentialPresentationGate().Inspect(context);
        }
        finally
        {
            received.Response.Close();
        }
    }

    static System.Net.HttpListener StartListener(string host, out int port)
    {
        for (var attempt = 1; ; attempt++)
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            var listener = new System.Net.HttpListener();
            listener.Prefixes.Add($"http://{host}:{port}/");
            try
            {
                listener.Start();
                return listener;
            }
            catch (HttpListenerException)
            {
                listener.Close();
                if (attempt == 3)
                {
                    throw;
                }
            }
        }
    }
}
