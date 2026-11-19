// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;
using System.Text;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Http.for_HttpListenerRequestContext.given;

public class a_header_tenant_resolver : an_http_listener_request_context
{
    protected HeaderTenantIdResolver _resolver;
    TcpClient _connection;

    protected async Task SendTenantHeaders(params string[] values)
    {
        var contextTask = _listener.GetContextAsync();
        _connection = new TcpClient();
        await _connection.ConnectAsync("localhost", _port);
        var headers = string.Join("\r\n", values.Select(value => $"{Constants.DefaultTenantIdHeader}: {value}"));
        var request = $"GET /test HTTP/1.1\r\nHost: localhost:{_port}\r\n{headers}\r\nConnection: close\r\n\r\n";
        await _connection.GetStream().WriteAsync(Encoding.ASCII.GetBytes(request));
        _context = await contextTask;
        _requestContext = new HttpListenerRequestContext(_context, _serviceProvider);
        var accessor = Substitute.For<IHttpRequestContextAccessor>();
        accessor.Current.Returns(_requestContext);
        _resolver = new HeaderTenantIdResolver(accessor, Options.Create(new ArcOptions()));
    }

    void Destroy() => _connection?.Dispose();
}
