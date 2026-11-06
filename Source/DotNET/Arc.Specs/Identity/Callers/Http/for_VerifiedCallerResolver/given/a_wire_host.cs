// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;
using System.Text;
using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity.Callers.Http.for_VerifiedCallerResolver.given;

public class a_wire_host : Specification
{
    internal CallerResolution _result;
    internal int _authorizationValues;
    internal IVerifiedCredentialSource _source;

    internal async Task ResolveWire(string headers, bool rewriteAuthorization = false)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        await using var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        _source = Substitute.For<IVerifiedCredentialSource>();
        _source.Shape.Returns(PresentedCredentialShape.OAuthBearer);
        app.MapPost("/mcp", async http =>
        {
            var context = new AspNetCoreHttpRequestContext(http);
            _authorizationValues = context.GetHeaderValues("Authorization").Count;
            var accessor = Substitute.For<IHttpRequestContextAccessor>();
            accessor.Current.Returns(context);
            var resolver = new VerifiedCallerResolver(accessor, [_source], new(), new(new(new Dictionary<string, string>())), new(null, null), Substitute.For<ITenantIdAccessor>());
            _result = await resolver.GetCurrent();
            if (rewriteAuthorization)
            {
                http.Request.Headers.Authorization = "Bearer changed-fixture";
                _result = await resolver.GetCurrent();
            }
            http.Response.StatusCode = _result.Refusal!.Value.HttpStatus();
        });
        await app.StartAsync();
        try
        {
            var address = new Uri(app.Urls.Single());
            using var client = new TcpClient();
            await client.ConnectAsync(address.Host, address.Port);
            var wire = $"POST /mcp HTTP/1.1\r\nHost: {address.Host}:{address.Port}\r\nContent-Length: 0\r\nConnection: close\r\n{headers}\r\n";
            var stream = client.GetStream();
            await stream.WriteAsync(Encoding.ASCII.GetBytes(wire));
            using var reader = new StreamReader(stream);
            await reader.ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            await app.StopAsync();
        }
    }
}
