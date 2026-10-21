// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Encodings.Web;
using System.Text.Json;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Authorization.for_AspNetCoreEndpointFallbackAuthorization;

[Collection("UsesCurrentDirectory")]
public class when_a_guest_subscribes_through_the_hub
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_match_direct_query_access_on_both_transports(bool useFallbackPolicy)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddCratisArc(configureBuilder: arc => arc.WithoutControllers());
        builder.Services.AddAuthentication("Missing").AddScheme<AuthenticationSchemeOptions, MissingCredentials>("Missing", _ => { });
        builder.Services.AddArcAuthorizationPolicy<AllowGuest>("HubGuest", evaluatesAnonymous: true);
        builder.Services.AddArcAnonymousAspNetAuthorizationPolicy("AspHubGuest");
        builder.Services.AddAuthorization(options =>
        {
            if (useFallbackPolicy)
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            }
            options.AddPolicy("AspHubGuest", policy => policy.RequireAssertion(_ => true));
        });
        var probe = new Probe();
        builder.Services.AddSingleton(probe);
        RegisterQueries(builder.Services);
        await using var app = builder.Build();
        app.UseWebSockets();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCratisArc();
        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(10) };
            var json = app.Services.GetRequiredService<IOptions<ArcOptions>>().Value.JsonSerializerOptions;
            foreach (var name in new[] { nameof(HubReadModel.Plain), nameof(HubReadModel.Anonymous), nameof(HubReadModel.Guest), nameof(HubReadModel.AspGuest) })
            {
                var allowed = name != nameof(HubReadModel.Plain) || !useFallbackPolicy;
                var before = probe.Queries;
                using var direct = await client.GetAsync($"/hub-fallback/{name}");
                direct.StatusCode.ShouldEqual(allowed ? HttpStatusCode.OK : HttpStatusCode.Unauthorized);
                await VerifyWebSocket(address, name, allowed, json);
                await VerifySse(client, name, allowed, json);
                probe.Queries.ShouldEqual(before + (allowed ? 3 : 0));
            }
        }
        finally
        {
            await app.StopAsync();
        }
    }

    static async Task VerifyWebSocket(string address, string name, bool allowed, JsonSerializerOptions json)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var socket = new ClientWebSocket();
        var uri = new UriBuilder(address) { Scheme = "ws", Path = ObservableQueryDemultiplexerEndpointMapper.WebSocketRoute }.Uri;
        await socket.ConnectAsync(uri, deadline.Token);
        (await Receive(socket, json, deadline.Token)).Type.ShouldEqual(ObservableQueryHubMessageType.Connected);
        var message = new ObservableQueryHubMessage
        {
            Type = ObservableQueryHubMessageType.Subscribe,
            QueryId = "snapshot",
            Payload = new ObservableQuerySubscriptionRequest($"{typeof(HubReadModel).FullName}.{name}")
        };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(message, json);
        await socket.SendAsync(new ArraySegment<byte>(bytes), System.Net.WebSockets.WebSocketMessageType.Text, true, deadline.Token);
        var result = await Receive(socket, json, deadline.Token);
        result.QueryId.ShouldEqual("snapshot");
        result.Type.ShouldEqual(allowed ? ObservableQueryHubMessageType.QueryResult : ObservableQueryHubMessageType.Unauthorized);
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", deadline.Token);
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

    static async Task VerifySse(HttpClient client, string name, bool allowed, JsonSerializerOptions json)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var stream = await client.GetAsync(ObservableQueryDemultiplexerEndpointMapper.SseRoute, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        stream.StatusCode.ShouldEqual(HttpStatusCode.OK);
        using var reader = new StreamReader(await stream.Content.ReadAsStreamAsync(deadline.Token));
        var connected = await ReadSse(reader, json, deadline.Token);
        connected.Type.ShouldEqual(ObservableQueryHubMessageType.Connected);
        var connectionId = ((JsonElement)connected.Payload!).GetString()!;
        using var subscribe = await client.PostAsJsonAsync(
            ObservableQueryDemultiplexerEndpointMapper.SseSubscribeRoute,
            new ObservableQuerySSESubscribeRequest(connectionId, "snapshot", new ObservableQuerySubscriptionRequest($"{typeof(HubReadModel).FullName}.{name}")),
            json,
            deadline.Token);
        subscribe.StatusCode.ShouldEqual(allowed ? HttpStatusCode.OK : HttpStatusCode.Unauthorized);
        var result = await ReadSse(reader, json, deadline.Token);
        result.QueryId.ShouldEqual("snapshot");
        result.Type.ShouldEqual(allowed ? ObservableQueryHubMessageType.QueryResult : ObservableQueryHubMessageType.Unauthorized);
    }

    static async Task<ObservableQueryHubMessage> ReadSse(StreamReader reader, JsonSerializerOptions json, CancellationToken token)
    {
        while (await reader.ReadLineAsync(token) is { } line)
        {
            if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                return JsonSerializer.Deserialize<ObservableQueryHubMessage>(line["data: ".Length..], json)!;
            }
        }

        throw new EndOfStreamException("The hub closed before sending its subscription result.");
    }

    static void RegisterQueries(IServiceCollection services) => services.AddSingleton<IQueryPerformerProviders>(provider =>
    {
        var performers = typeof(HubReadModel).GetMethods().Where(method => method.IsStatic && method.IsDefined(typeof(PathAttribute), false))
            .Select(method => (IQueryPerformer)new ModelBoundQueryPerformer(
                typeof(HubReadModel),
                typeof(HubReadModel).FullName!,
                method,
                provider.GetRequiredService<IServiceProviderIsService>(),
                provider.GetRequiredService<IAuthorizationEvaluator>())).ToArray();
        var catalog = Substitute.For<IQueryPerformerProviders>();
        catalog.Performers.Returns(performers);
        catalog.TryGetPerformersFor(Arg.Any<FullyQualifiedQueryName>(), out Arg.Any<IQueryPerformer?>()).Returns(call =>
        {
            var found = performers.SingleOrDefault(performer => performer.FullyQualifiedName == (FullyQualifiedQueryName)call[0]);
            call[1] = found;
            return found is not null;
        });
        return catalog;
    });

    public class Probe
    {
        public int Queries { get; set; }
    }

    public class AllowGuest : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }

    public record HubReadModel
    {
        [Path("/hub-fallback/Plain")]
        public static int Plain(Probe probe) => ++probe.Queries;

        [Path("/hub-fallback/Anonymous"), AllowAnonymous]
        public static int Anonymous(Probe probe) => ++probe.Queries;

        [Path("/hub-fallback/Guest"), Authorize(Policy = "HubGuest")]
        public static int Guest(Probe probe) => ++probe.Queries;

        [Path("/hub-fallback/AspGuest"), Microsoft.AspNetCore.Authorization.Authorize(Policy = "AspHubGuest")]
        public static int AspGuest(Probe probe) => ++probe.Queries;
    }

    class MissingCredentials(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
    }
}
