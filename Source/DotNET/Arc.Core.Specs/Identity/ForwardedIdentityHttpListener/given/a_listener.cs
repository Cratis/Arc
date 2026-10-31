// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using System.Text;
using Cratis.Arc.Authentication;
using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_ForwardedIdentityHttpListener.given;

public class a_listener : Specification
{
    protected AuthenticationResult _reentered;
    protected string[] _observedNames;

    protected async Task<AuthenticationResult> Authenticate(Dictionary<string, string> headers, bool enabled = true, string? appendedName = null, bool reenter = false, ArcOptions? options = null)
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        var host = OperatingSystem.IsWindows() ? "localhost" : "127.0.0.1";
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://{host}:{port}/");
        listener.Start();
        using var client = new TcpClient();
        await client.ConnectAsync(host, port);
        var request = new StringBuilder($"POST /base/a%2fb?x=1 HTTP/1.1\r\nHost: {host}:{port}\r\nContent-Length: 0\r\n");
        foreach (var pair in headers)
        {
            request.Append(pair.Key).Append(": ").Append(pair.Value).Append("\r\n");
        }
        if (appendedName is not null)
        {
            request.Append("X-MS-CLIENT-PRINCIPAL-NAME: ").Append(appendedName).Append("\r\n");
        }
        request.Append("\r\n");
        await client.GetStream().WriteAsync(Encoding.ASCII.GetBytes(request.ToString()));
        var received = await listener.GetContextAsync().WaitAsync(TimeSpan.FromSeconds(10));
        await using var services = new ServiceCollection().BuildServiceProvider();
        var context = new HttpListenerRequestContext(received, services);
        _observedNames = context.GetHeaderValues(MicrosoftIdentityPlatformHeaders.IdentityNameHeader).ToArray();
        options ??= ForwardedIdentityForSpecs.Options();
        options.ForwardedIdentity.Enabled = enabled;
        var handler = new MicrosoftIdentityPlatformAuthenticationHandler(Options.Create(options), NullLoggerFactory.Instance);
        var result = await handler.HandleAuthentication(context);
        if (reenter)
        {
            _reentered = await handler.HandleAuthentication(context);
        }
        received.Response.Close();
        return result;
    }
}
