// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_WebSocketOriginGate;

/// <summary>
/// A real ASP.NET Core host, with Arc's full pipeline and Kestrel, used to connect over real sockets.
/// </summary>
public sealed class an_origin_probe_host : IAsyncDisposable
{
    public const string Same = "same";
    const string ValidKey = "dGhlIHNhbXBsZSBub25jZQ==";

    readonly WebApplication _app;

    an_origin_probe_host(WebApplication app, string address, OriginProbe probe)
    {
        _app = app;
        Address = address;
        Probe = probe;
    }

    public string Address { get; }

    public OriginProbe Probe { get; }

    public static async Task<an_origin_probe_host> Start(IList<string>? allowed, bool withControllers, Action<IServiceCollection>? configure = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        if (withControllers)
        {
            builder.AddCratisArc();
        }
        else
        {
            builder.AddCratisArc(configureBuilder: arc => arc.WithoutControllers());
        }

        var probe = new OriginProbe();
        builder.Services.AddSingleton(probe);
        builder.Services.Configure<ArcOptions>(options => options.Query.AllowedWebSocketOrigins = allowed);
        configure?.Invoke(builder.Services);
        var app = builder.Build();
        app.UseWebSockets();
        app.UseCratisArc();
        if (withControllers)
        {
            app.MapControllers();
        }

        await app.StartAsync(global::Xunit.TestContext.Current.CancellationToken);
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        return new an_origin_probe_host(app, address, probe);
    }

    public async Task<bool> Connects(string path, string? origin)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var socket = new ClientWebSocket();
        if (origin is not null)
        {
            socket.Options.SetRequestHeader("Origin", origin == Same ? Address : origin);
        }

        try
        {
            await socket.ConnectAsync(new UriBuilder(Address) { Scheme = "ws", Path = path }.Uri, deadline.Token);
        }
        catch (WebSocketException)
        {
            return false;
        }

        await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "done", deadline.Token);
        return true;
    }

    public static void RegisterHubQuery(IServiceCollection services) => services.AddSingleton<IQueryPerformerProviders>(provider =>
    {
        var method = typeof(OriginHubReadModel).GetMethod(nameof(OriginHubReadModel.Query))!;
        var performer = (IQueryPerformer)new ModelBoundQueryPerformer(
            typeof(OriginHubReadModel),
            typeof(OriginHubReadModel).FullName!,
            method,
            provider.GetRequiredService<IServiceProviderIsService>(),
            provider.GetRequiredService<IAuthorizationEvaluator>());
        var catalog = Substitute.For<IQueryPerformerProviders>();
        catalog.Performers.Returns([performer]);
        catalog.TryGetPerformersFor(Arg.Any<FullyQualifiedQueryName>(), out Arg.Any<IQueryPerformer?>()).Returns(call =>
        {
            call[1] = performer;
            return performer.FullyQualifiedName == (FullyQualifiedQueryName)call[0];
        });
        return catalog;
    });

    public async Task<bool> SubscribesToHubQuery(string? origin)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var socket = new ClientWebSocket();
        if (origin is not null)
        {
            socket.Options.SetRequestHeader("Origin", origin == Same ? Address : origin);
        }

        var json = _app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ArcOptions>>().Value.JsonSerializerOptions;
        try
        {
            await socket.ConnectAsync(new UriBuilder(Address) { Scheme = "ws", Path = ObservableQueryDemultiplexerEndpointMapper.WebSocketRoute }.Uri, deadline.Token);
        }
        catch (WebSocketException)
        {
            return false;
        }

        await Receive(socket, json, deadline.Token);
        var message = new ObservableQueryHubMessage
        {
            Type = ObservableQueryHubMessageType.Subscribe,
            QueryId = "snapshot",
            Payload = new ObservableQuerySubscriptionRequest(typeof(OriginHubReadModel).FullName + "." + nameof(OriginHubReadModel.Query))
        };
        await socket.SendAsync(new ArraySegment<byte>(JsonSerializer.SerializeToUtf8Bytes(message, json)), System.Net.WebSockets.WebSocketMessageType.Text, true, deadline.Token);
        (await Receive(socket, json, deadline.Token)).Type.ShouldEqual(ObservableQueryHubMessageType.QueryResult);
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", deadline.Token);
        return true;
    }

    static async Task<ObservableQueryHubMessage> Receive(ClientWebSocket socket, JsonSerializerOptions json, CancellationToken token)
    {
        await using var data = new MemoryStream();
        var buffer = new byte[4096];
        WebSocketReceiveResult received;
        do
        {
            received = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
            await data.WriteAsync(buffer.AsMemory(0, received.Count), token);
        }
        while (!received.EndOfMessage);

        return JsonSerializer.Deserialize<ObservableQueryHubMessage>(data.ToArray(), json)!;
    }

    public async Task<HttpStatusCode> Upgrade(string path, string? origin, string? key = null)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var client = new HttpClient { BaseAddress = new Uri(Address), Timeout = TimeSpan.FromSeconds(10) };
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("Connection", "Upgrade");
        request.Headers.TryAddWithoutValidation("Upgrade", "websocket");
        request.Headers.TryAddWithoutValidation("Sec-WebSocket-Version", "13");
        request.Headers.TryAddWithoutValidation("Sec-WebSocket-Key", key ?? ValidKey);
        if (origin is not null)
        {
            request.Headers.TryAddWithoutValidation("Origin", origin == Same ? Address : origin);
        }

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        return response.StatusCode;
    }

    public async ValueTask DisposeAsync()
    {
        // Finish host teardown even when test-runner cancellation has fired.
#pragma warning disable xUnit1051
        await _app.StopAsync();
#pragma warning restore xUnit1051
        await _app.DisposeAsync();
    }
}
