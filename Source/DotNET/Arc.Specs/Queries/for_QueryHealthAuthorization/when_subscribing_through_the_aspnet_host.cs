// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
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

namespace Cratis.Arc.Queries.for_QueryHealthAuthorization;

[Collection("UsesCurrentDirectory")]
public class when_subscribing_through_the_aspnet_host
{
    static readonly Dictionary<AccessCase, (string EnvironmentName, bool? RequireAuthentication, string? Roles, string? CallerRole, bool UseFallback, bool Allowed)> _settings = new()
    {
        [AccessCase.Production] = ("Production", null, null, null, false, false),
        [AccessCase.ProductionWithFallback] = ("Production", null, null, null, true, false),
        [AccessCase.DevelopmentWithFallback] = ("Development", null, null, null, true, true),
        [AccessCase.ExplicitAuthentication] = ("Development", true, null, null, false, false),
        [AccessCase.ExplicitAnonymousWithFallback] = ("Production", false, null, null, true, true),
        [AccessCase.Authenticated] = ("Production", true, null, "Reader", true, true),
        [AccessCase.MatchingRole] = ("Production", true, "Admin, Reader", "Reader", true, true),
        [AccessCase.MissingRole] = ("Production", true, "Admin", "Reader", true, false)
    };

    public enum AccessCase
    {
        Production = 0,
        ProductionWithFallback = 1,
        DevelopmentWithFallback = 2,
        ExplicitAuthentication = 3,
        ExplicitAnonymousWithFallback = 4,
        Authenticated = 5,
        MatchingRole = 6,
        MissingRole = 7
    }

    [Theory]
    [InlineData(AccessCase.Production)]
    [InlineData(AccessCase.ProductionWithFallback)]
    [InlineData(AccessCase.DevelopmentWithFallback)]
    [InlineData(AccessCase.ExplicitAuthentication)]
    [InlineData(AccessCase.ExplicitAnonymousWithFallback)]
    [InlineData(AccessCase.Authenticated)]
    [InlineData(AccessCase.MatchingRole)]
    [InlineData(AccessCase.MissingRole)]
    public async Task should_protect_health_on_direct_http_and_both_hub_transports(AccessCase accessCase)
    {
        var (environmentName, requireAuthentication, roles, callerRole, useFallback, allowed) = _settings[accessCase];
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environmentName });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddCratisArc(configureBuilder: arc => arc.WithoutControllers());
        builder.Services.Configure<ArcOptions>(options =>
        {
            if (requireAuthentication is { } required)
            {
                options.QueryHealth.RequireAuthentication = required;
            }
            options.QueryHealth.Roles = roles;
        });
        builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, Credentials>("Test", _ => { });
        builder.Services.AddAuthorization(options =>
        {
            if (useFallback)
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            }
        });
        RegisterQueries(builder.Services);
        await using var app = builder.Build();
        app.UseWebSockets();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCratisArc();
        await app.StartAsync(global::Xunit.TestContext.Current.CancellationToken);
        try
        {
            var address = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());
            using var client = new HttpClient { BaseAddress = address, Timeout = TimeSpan.FromSeconds(10) };
            if (callerRole is not null)
            {
                client.DefaultRequestHeaders.Add("X-Test-Role", callerRole);
            }
            var json = app.Services.GetRequiredService<IOptions<ArcOptions>>().Value.JsonSerializerOptions;
            var healthName = $"{typeof(QueryHealth).FullName}.{nameof(QueryHealth.ObserveHealth)}";
            using var direct = await client.GetAsync("/.cratis/queries/health?waitForFirstResult=true", HttpCompletionOption.ResponseHeadersRead, global::Xunit.TestContext.Current.CancellationToken);
            var deniedStatus = useFallback && callerRole is null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;
            direct.StatusCode.ShouldEqual(allowed ? HttpStatusCode.OK : deniedStatus);
            await VerifyWebSocket(address, (healthName, callerRole, allowed), json);
            await VerifySse(client, healthName, allowed, json);
            var plainAllowed = callerRole is not null || !useFallback;
            await VerifyWebSocket(address, ($"{typeof(OtherQueries).FullName}.{nameof(OtherQueries.Plain)}", callerRole, plainAllowed), json);
            await VerifySse(client, $"{typeof(OtherQueries).FullName}.{nameof(OtherQueries.Plain)}", plainAllowed, json);
            await VerifyWebSocket(address, ($"{typeof(OtherQueries).FullName}.{nameof(OtherQueries.Anonymous)}", callerRole, true), json);
            await VerifySse(client, $"{typeof(OtherQueries).FullName}.{nameof(OtherQueries.Anonymous)}", true, json);
        }
        finally
        {
            // Finish host teardown even when test-runner cancellation has fired.
#pragma warning disable xUnit1051
            await app.StopAsync();
#pragma warning restore xUnit1051
        }
    }

    static async Task VerifyWebSocket(Uri address, (string Name, string? Role, bool Allowed) access, JsonSerializerOptions json)
    {
        var (name, role, allowed) = access;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var socket = new ClientWebSocket();
        if (role is not null)
        {
            socket.Options.SetRequestHeader("X-Test-Role", role);
        }
        await socket.ConnectAsync(new UriBuilder(address) { Scheme = "ws", Path = ObservableQueryDemultiplexerEndpointMapper.WebSocketRoute }.Uri, deadline.Token);
        (await Receive(socket, json, deadline.Token)).Type.ShouldEqual(ObservableQueryHubMessageType.Connected);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new ObservableQueryHubMessage
        {
            Type = ObservableQueryHubMessageType.Subscribe,
            QueryId = "health",
            Payload = new ObservableQuerySubscriptionRequest(name)
        },
        json);
        await socket.SendAsync(new ArraySegment<byte>(bytes), System.Net.WebSockets.WebSocketMessageType.Text, true, deadline.Token);
        var result = await Receive(socket, json, deadline.Token);
        result.QueryId.ShouldEqual("health");
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
            new ObservableQuerySSESubscribeRequest(connectionId, "health", new ObservableQuerySubscriptionRequest(name)),
            json,
            deadline.Token);
        subscribe.StatusCode.ShouldEqual(allowed ? HttpStatusCode.OK : HttpStatusCode.Unauthorized);
        var result = await ReadSse(reader, json, deadline.Token);
        result.QueryId.ShouldEqual("health");
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
        var methods = new[] { typeof(QueryHealth).GetMethod(nameof(QueryHealth.ObserveHealth))!, typeof(OtherQueries).GetMethod(nameof(OtherQueries.Plain))!, typeof(OtherQueries).GetMethod(nameof(OtherQueries.Anonymous))! };
        var performers = methods.Select(method => (IQueryPerformer)new ModelBoundQueryPerformer(
            method.DeclaringType!,
            method.DeclaringType!.FullName!,
            method,
            provider.GetRequiredService<IServiceProviderIsService>(),
            provider.GetRequiredService<Authorization.IAuthorizationEvaluator>())).ToArray();
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

    public record OtherQueries
    {
        [Path("/health-spec/plain")]
        public static int Plain() => 42;

        [Path("/health-spec/anonymous"), Authorization.AllowAnonymous]
        public static int Anonymous() => 42;
    }

    class Credentials(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(
            Request.Headers["X-Test-Role"].FirstOrDefault() is { } role
                ? AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "Test")), "Test"))
                : AuthenticateResult.NoResult());
    }
}
