// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Cratis.Arc.Hosting.for_KestrelPorts;

[Collection("UsesCurrentDirectory")]
public class when_an_address_is_taken_before_kestrel_binds : Specification
{
    int _attempts;
    int[] _ports;
    HttpStatusCode _status;
    string _body;

    async Task Because()
    {
        TcpListener? occupied = null;
        try
        {
            await using var app = await KestrelPorts.Start(
                2,
                ports =>
            {
                _attempts++;
                _ports = ports;
                if (_attempts == 1)
                {
                    occupied = new TcpListener(IPAddress.Loopback, ports[1]);
                    occupied.Start();
                }
                var builder = WebApplication.CreateBuilder();
                builder.WebHost.UseUrls($"http://127.0.0.1:{ports[0]}", $"http://127.0.0.1:{ports[1]}");
                var host = builder.Build();
                host.MapGet("/", () => "listening");
                return host;
            },
                global::Xunit.TestContext.Current.CancellationToken);
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            using var response = await client.GetAsync($"http://127.0.0.1:{_ports[1]}/", global::Xunit.TestContext.Current.CancellationToken);
            _status = response.StatusCode;
            _body = await response.Content.ReadAsStringAsync(global::Xunit.TestContext.Current.CancellationToken);
        }
        finally
        {
            occupied?.Stop();
        }
    }

    [Fact] void should_rebuild_after_the_bind_collision() => _attempts.ShouldBeGreaterThanOrEqual(2);
    [Fact] void should_select_distinct_ports_for_the_host() => _ports.Distinct().Count().ShouldEqual(2);
    [Fact] void should_serve_requests_on_the_retried_host() => _status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_return_the_endpoint_response() => _body.ShouldEqual("listening");
}
