// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using Cratis.Arc.Authentication;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Http;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_GuestPolicyEndpointAccess;

[Collection("UsesCurrentDirectory")]
public class when_a_guest_calls_the_core_host
{
    [Theory]
    [InlineData("guest", HttpStatusCode.OK)]
    [InlineData("denied", HttpStatusCode.Forbidden)]
    [InlineData("ordinary", HttpStatusCode.Unauthorized)]
    [InlineData("mixed", HttpStatusCode.Unauthorized)]
    [InlineData("roles", HttpStatusCode.Unauthorized)]
    [InlineData("authenticated", HttpStatusCode.Unauthorized)]
    [InlineData("unannotated", HttpStatusCode.Unauthorized)]
    public async Task should_only_admit_guest_policy_endpoints(string path, HttpStatusCode expected)
    {
        var (app, address, probe) = BuildHost();
        await using var host = app;
        await app.StartAsync(global::Xunit.TestContext.Current.CancellationToken);
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(10) };
            using var command = await client.PostAsJsonAsync($"/guest-spec/{path}", new { }, global::Xunit.TestContext.Current.CancellationToken);
            await ShouldHaveStatus(command, expected, "command");
            using var validation = await client.PostAsJsonAsync($"/guest-spec/{path}/validate", new { }, global::Xunit.TestContext.Current.CancellationToken);
            await ShouldHaveStatus(validation, expected, "validation");
            using var query = await client.GetAsync($"/guest-query/{path}", global::Xunit.TestContext.Current.CancellationToken);
            await ShouldHaveStatus(query, expected, "query");
            using var queryRequest = new HttpRequestMessage(new HttpMethod("QUERY"), $"/guest-query/{path}") { Content = JsonContent.Create(new { }) };
            using var bodyQuery = await client.SendAsync(queryRequest, global::Xunit.TestContext.Current.CancellationToken);
            await ShouldHaveStatus(bodyQuery, expected, "body query");
            probe.Commands.ShouldEqual(expected == HttpStatusCode.OK ? 1 : 0);
            probe.Queries.ShouldEqual(expected == HttpStatusCode.OK ? 2 : 0);
            probe.GuestPolicyCalls.ShouldEqual(expected is HttpStatusCode.OK or HttpStatusCode.Forbidden ? 4 : 0);
        }
        finally
        {
            // Finish host teardown even when test-runner cancellation has fired.
#pragma warning disable xUnit1051
            await app.StopAsync();
#pragma warning restore xUnit1051
        }
    }

    [Fact]
    public async Task should_refuse_a_request_whose_body_arrives_late_without_corrupting_the_connection()
    {
        var responses = await SendEarlyRejectedRequest("Content-Length: 2\r\n", "{}");
        responses.Contains("400 Bad Request", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    public async Task should_refuse_a_request_whose_chunked_body_arrives_late_without_corrupting_the_connection()
    {
        var responses = await SendEarlyRejectedRequest("Transfer-Encoding: chunked\r\n", "2\r\n{}\r\n0\r\n\r\n");
        responses.Contains("400 Bad Request", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    public async Task should_close_the_connection_when_an_early_rejected_body_is_oversized()
    {
        var body = new string('a', 256 * 1024);
        var responses = await SendEarlyRejectedRequest($"Content-Length: {body.Length}\r\n", body);
        responses.Contains("400 Bad Request", StringComparison.Ordinal).ShouldBeFalse();
        (responses.Split("HTTP/1.1 ").Length <= 2).ShouldBeTrue();
    }

    static async Task<string> SendEarlyRejectedRequest(string framingHeader, string body)
    {
        var (app, address, _) = BuildHost();
        await using var host = app;
        await app.StartAsync(global::Xunit.TestContext.Current.CancellationToken);
        try
        {
            var uri = new Uri(address);
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(uri.Host, uri.Port, global::Xunit.TestContext.Current.CancellationToken);
            var stream = tcp.GetStream();
            var request = $"POST /guest-spec/ordinary/validate HTTP/1.1\r\nHost: {uri.Authority}\r\nContent-Type: application/json\r\n{framingHeader}\r\n";
            await stream.WriteAsync(Encoding.ASCII.GetBytes(request), global::Xunit.TestContext.Current.CancellationToken);
            await Task.Delay(300, global::Xunit.TestContext.Current.CancellationToken);
            try
            {
                await stream.WriteAsync(Encoding.ASCII.GetBytes(body), global::Xunit.TestContext.Current.CancellationToken);
                await Task.Delay(300, global::Xunit.TestContext.Current.CancellationToken);
                var followUp = $"GET /guest-query/ordinary HTTP/1.1\r\nHost: {uri.Authority}\r\nConnection: close\r\n\r\n";
                await stream.WriteAsync(Encoding.ASCII.GetBytes(followUp), global::Xunit.TestContext.Current.CancellationToken);
                using var reader = new StreamReader(stream, Encoding.ASCII);
                return await reader.ReadToEndAsync(global::Xunit.TestContext.Current.CancellationToken);
            }
            catch (IOException)
            {
                // The server may close the connection while the body is still being sent.
                return string.Empty;
            }
        }
        finally
        {
#pragma warning disable xUnit1051
            await app.StopAsync();
#pragma warning restore xUnit1051
        }
    }

    static (ArcApplication App, string Address, Probe Probe) BuildHost()
    {
        using var portProbe = new TcpListener(IPAddress.Loopback, 0);
        portProbe.Start();
        var port = ((IPEndPoint)portProbe.LocalEndpoint).Port;
        portProbe.Stop();
        var address = $"http://127.0.0.1:{port}/";
        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc(options => options.Hosting.ApplicationUrl = address);
        builder.Services.AddArcAuthorizationPolicy<AllowGuest>("Guest", evaluatesAnonymous: true);
        builder.Services.AddArcAuthorizationPolicy<DenyGuest>("Denied", evaluatesAnonymous: true);
        builder.Services.AddArcAuthorizationPolicy<AllowGuest>("Ordinary");
        var authenticationHandlers = Substitute.For<IInstancesOf<IAuthenticationHandler>>();
        authenticationHandlers.GetEnumerator().Returns(_ => new IAuthenticationHandler[] { new MissingCredentials() }.AsEnumerable().GetEnumerator());
        builder.Services.AddSingleton(authenticationHandlers);
        var probe = new Probe();
        builder.Services.AddSingleton(probe);
        RegisterArtifacts(builder.Services);
        var app = builder.Build();
        app.UseCratisArc();
        return (app, address, probe);
    }

    static async Task ShouldHaveStatus(HttpResponseMessage response, HttpStatusCode expected, string request)
    {
        if (response.StatusCode == expected)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(global::Xunit.TestContext.Current.CancellationToken);
        throw new InvalidOperationException($"The {request} request returned {response.StatusCode} instead of {expected}: {body}");
    }

    static void RegisterArtifacts(IServiceCollection services)
    {
        var handlers = new[] { typeof(GuestCommand), typeof(DeniedCommand), typeof(OrdinaryCommand), typeof(MixedCommand), typeof(RoleCommand), typeof(AuthenticatedCommand), typeof(UnannotatedCommand) }
            .Select(type => (ICommandHandler)new ModelBoundCommandHandler(type, type.GetMethod("Handle")!)).ToArray();
        var commands = Substitute.For<ICommandHandlerProviders>();
        commands.Handlers.Returns(handlers);
        commands.TryGetHandlerFor(Arg.Any<object>(), out Arg.Any<ICommandHandler?>()).Returns(call =>
        {
            var found = handlers.SingleOrDefault(handler => handler.CommandType == call[0]!.GetType());
            call[1] = found;
            return found is not null;
        });
        services.AddSingleton(commands);
        services.AddSingleton(Substitute.For<ICommandKeys>());
        services.AddSingleton<IQueryPerformerProviders>(provider =>
        {
            var performers = typeof(GuestReadModel).GetMethods().Where(method => method.IsStatic && method.IsDefined(typeof(PathAttribute), false))
                .Select(method => (IQueryPerformer)new ModelBoundQueryPerformer(
                    typeof(GuestReadModel),
                    typeof(GuestReadModel).FullName!,
                    method,
                    provider.GetRequiredService<IServiceProviderIsService>(),
                    provider.GetRequiredService<IAuthorizationEvaluator>())).ToArray();
            var queries = Substitute.For<IQueryPerformerProviders>();
            queries.Performers.Returns(performers);
            queries.TryGetPerformersFor(Arg.Any<FullyQualifiedQueryName>(), out Arg.Any<IQueryPerformer?>()).Returns(call =>
            {
                var found = performers.SingleOrDefault(performer => performer.FullyQualifiedName == (FullyQualifiedQueryName)call[0]);
                call[1] = found;
                return found is not null;
            });
            return queries;
        });
    }

    public class Probe
    {
        public int Commands { get; set; }
        public int Queries { get; set; }
        public int GuestPolicyCalls { get; set; }
    }

    public class AllowGuest(Probe probe) : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            probe.GuestPolicyCalls++;
            return ValueTask.FromResult(context.Principal.Identities.All(identity => !identity.IsAuthenticated && !identity.Claims.Any()));
        }
    }

    public class DenyGuest(Probe probe) : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            probe.GuestPolicyCalls++;
            return ValueTask.FromResult(false);
        }
    }

    [Path("/guest-spec/guest"), Authorize(Policy = "Guest")]
    public record GuestCommand { public void Handle(Probe probe) => probe.Commands++; }

    [Path("/guest-spec/denied"), Authorize(Policy = "Denied")]
    public record DeniedCommand { public void Handle(Probe probe) => probe.Commands++; }

    [Path("/guest-spec/ordinary"), Authorize(Policy = "Ordinary")]
    public record OrdinaryCommand { public void Handle(Probe probe) => probe.Commands++; }

    [Path("/guest-spec/mixed"), Authorize(Policy = "Guest"), Authorize(Policy = "Ordinary")]
    public record MixedCommand { public void Handle(Probe probe) => probe.Commands++; }

    [Path("/guest-spec/roles"), Authorize(Policy = "Guest", Roles = "Admin")]
    public record RoleCommand { public void Handle(Probe probe) => probe.Commands++; }

    [Path("/guest-spec/authenticated"), Authorize]
    public record AuthenticatedCommand { public void Handle(Probe probe) => probe.Commands++; }

    [Path("/guest-spec/unannotated")]
    public record UnannotatedCommand { public void Handle(Probe probe) => probe.Commands++; }

    [Roles("Admin")]
    public record GuestReadModel
    {
        [Path("/guest-query/guest"), Authorize(Policy = "Guest")]
        public static int Guest(Probe probe) => ++probe.Queries;

        [Path("/guest-query/denied"), Authorize(Policy = "Denied")]
        public static int Denied(Probe probe) => ++probe.Queries;

        [Path("/guest-query/ordinary"), Authorize(Policy = "Ordinary")]
        public static int Ordinary(Probe probe) => ++probe.Queries;

        [Path("/guest-query/mixed"), Authorize(Policy = "Guest"), Authorize(Policy = "Ordinary")]
        public static int Mixed(Probe probe) => ++probe.Queries;

        [Path("/guest-query/roles"), Authorize(Policy = "Guest", Roles = "Admin")]
        public static int Roles(Probe probe) => ++probe.Queries;

        [Path("/guest-query/authenticated"), Authorize]
        public static int Authenticated(Probe probe) => ++probe.Queries;

        [Path("/guest-query/unannotated")]
        public static int Unannotated(Probe probe) => ++probe.Queries;
    }

    class MissingCredentials : IAuthenticationHandler
    {
        public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context) => Task.FromResult(AuthenticationResult.Anonymous);
    }
}
