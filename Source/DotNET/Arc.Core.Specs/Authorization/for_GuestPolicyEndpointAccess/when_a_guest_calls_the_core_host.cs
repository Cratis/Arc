// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
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
        await using var app = builder.Build();
        app.UseCratisArc();
        await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(10) };
            using var command = await client.PostAsJsonAsync($"/guest-spec/{path}", new { });
            command.StatusCode.ShouldEqual(expected);
            using var validation = await client.PostAsJsonAsync($"/guest-spec/{path}/validate", new { });
            validation.StatusCode.ShouldEqual(expected);
            using var query = await client.GetAsync($"/guest-query/{path}");
            query.StatusCode.ShouldEqual(expected);
            using var queryRequest = new HttpRequestMessage(new HttpMethod("QUERY"), $"/guest-query/{path}") { Content = JsonContent.Create(new { }) };
            using var bodyQuery = await client.SendAsync(queryRequest);
            bodyQuery.StatusCode.ShouldEqual(expected);
            probe.Commands.ShouldEqual(expected == HttpStatusCode.OK ? 1 : 0);
            probe.Queries.ShouldEqual(expected == HttpStatusCode.OK ? 2 : 0);
            probe.GuestPolicyCalls.ShouldEqual(expected is HttpStatusCode.OK or HttpStatusCode.Forbidden ? 4 : 0);
        }
        finally
        {
            await app.StopAsync();
        }
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
