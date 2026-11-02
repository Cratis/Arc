// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using Cratis.Arc.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_WebSocketOriginGate;

[Collection("UsesCurrentDirectory")]
public class when_connecting_through_the_aspnet_host
{
    const string Same = "same";

    [Theory]
    [InlineData("", "https://elsewhere.test", true)]
    [InlineData("https://trusted.test", null, true)]
    [InlineData("https://trusted.test", Same, true)]
    [InlineData("https://trusted.test", "https://trusted.test", true)]
    [InlineData("https://trusted.test", "https://elsewhere.test", false)]
    [InlineData("https://trusted.test", "null", false)]
    [InlineData("https://trusted.test", "", false)]
    [InlineData("*", "https://elsewhere.test", true)]
    public async Task should_apply_the_trusted_origins(string allowed, string? origin, bool connects)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddCratisArc(configureBuilder: arc => arc.WithoutControllers());
        builder.Services.Configure<ArcOptions>(options =>
        {
            if (allowed.Length > 0)
            {
                options.Query.AllowedWebSocketOrigins.Add(allowed);
            }
        });
        await using var app = builder.Build();
        app.UseWebSockets();
        app.UseCratisArc();
        await app.StartAsync(global::Xunit.TestContext.Current.CancellationToken);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var socket = new ClientWebSocket();
            if (origin is not null)
            {
                socket.Options.SetRequestHeader("Origin", origin == Same ? address : origin);
            }

            var uri = new UriBuilder(address) { Scheme = "ws", Path = ObservableQueryDemultiplexerEndpointMapper.WebSocketRoute }.Uri;
            if (connects)
            {
                await socket.ConnectAsync(uri, deadline.Token);
                socket.State.ShouldEqual(WebSocketState.Open);
            }
            else
            {
                var refused = false;
                try
                {
                    await socket.ConnectAsync(uri, deadline.Token);
                }
                catch (WebSocketException)
                {
                    refused = true;
                }

                refused.ShouldBeTrue();
                socket.State.ShouldNotEqual(WebSocketState.Open);
            }
        }
        finally
        {
#pragma warning disable xUnit1051
            await app.StopAsync();
#pragma warning restore xUnit1051
        }
    }
}
