// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
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

namespace Cratis.Arc.Authorization.for_GuestPolicyEndpointAccess;

[Collection("UsesCurrentDirectory")]
public class when_a_guest_calls_the_aspnet_host
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_only_admit_guest_policy_endpoints(bool useFallbackPolicy)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddCratisArc(configureBuilder: arc => arc.WithoutControllers());
        builder.Services.AddAuthentication("Missing").AddScheme<AuthenticationSchemeOptions, MissingCredentials>("Missing", _ => { });
        var probe = new Probe();
        builder.Services.AddSingleton(probe);
        builder.Services.AddArcAuthorizationPolicy<AllowGuest>("NativeGuest", evaluatesAnonymous: true);
        builder.Services.AddArcAuthorizationPolicy<AllowGuest>("NativeOrdinary");
        builder.Services.AddArcAnonymousAspNetAuthorizationPolicy("AspGuest");
        builder.Services.AddArcAnonymousAspNetAuthorizationPolicy("AspDenied");
        builder.Services.AddAuthorization(options =>
        {
            if (useFallbackPolicy)
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            }
            options.AddPolicy("AspGuest", policy => policy.RequireAssertion(context =>
            {
                probe.AspPolicyCalls++;
                return context.User.Identities.All(identity => !identity.IsAuthenticated && !identity.Claims.Any());
            }));
            options.AddPolicy("AspDenied", policy => policy.RequireAssertion(_ =>
            {
                probe.AspPolicyCalls++;
                return false;
            }));
            options.AddPolicy("AspOrdinary", policy => policy.RequireAssertion(_ => true));
        });
        RegisterArtifacts(builder.Services);
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCratisArc();
        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(10) };
            foreach (var path in new[] { "native", "asp", "both", "denied", "ordinary", "mixed", "roles", "schemes", "authenticated", "unannotated" })
            {
                var guestPolicy = new[] { "native", "asp", "both", "denied" }.Contains(path, StringComparer.Ordinal);
                var expected = new[] { "native", "asp", "both" }.Contains(path, StringComparer.Ordinal) || (path == "unannotated" && !useFallbackPolicy)
                    ? HttpStatusCode.OK
                    : useFallbackPolicy && !guestPolicy ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;
                var commandsBefore = probe.Commands;
                var queriesBefore = probe.Queries;
                var nativeBefore = probe.NativePolicyCalls;
                var aspBefore = probe.AspPolicyCalls;
                using var command = await client.PostAsJsonAsync($"/guest-spec/{path}", new { });
                command.StatusCode.ShouldEqual(expected);
                using var validation = await client.PostAsJsonAsync($"/guest-spec/{path}/validate", new { });
                validation.StatusCode.ShouldEqual(expected);
                using var query = await client.GetAsync($"/guest-query/{path}");
                query.StatusCode.ShouldEqual(expected);
                using var queryRequest = new HttpRequestMessage(new HttpMethod("QUERY"), $"/guest-query/{path}") { Content = JsonContent.Create(new { }) };
                using var bodyQuery = await client.SendAsync(queryRequest);
                bodyQuery.StatusCode.ShouldEqual(expected);
                probe.Commands.ShouldEqual(commandsBefore + (expected == HttpStatusCode.OK ? 1 : 0));
                probe.Queries.ShouldEqual(queriesBefore + (expected == HttpStatusCode.OK ? 2 : 0));
                probe.NativePolicyCalls.ShouldEqual(nativeBefore + (new[] { "native", "both" }.Contains(path, StringComparer.Ordinal) ? 4 : 0));
                probe.AspPolicyCalls.ShouldEqual(aspBefore + (new[] { "asp", "both", "denied" }.Contains(path, StringComparer.Ordinal) ? 4 : 0));
            }
        }
        finally
        {
            await app.StopAsync();
        }
    }

    static void RegisterArtifacts(IServiceCollection services)
    {
        var handlers = new[] { typeof(NativeCommand), typeof(AspCommand), typeof(BothCommand), typeof(DeniedCommand), typeof(OrdinaryCommand), typeof(MixedCommand), typeof(RoleCommand), typeof(SchemeCommand), typeof(AuthenticatedCommand), typeof(UnannotatedCommand) }
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
        public int NativePolicyCalls { get; set; }
        public int AspPolicyCalls { get; set; }
    }

    public class AllowGuest(Probe probe) : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            probe.NativePolicyCalls++;
            return ValueTask.FromResult(context.Principal.Identities.All(identity => !identity.IsAuthenticated && !identity.Claims.Any()));
        }
    }

    [Path("/guest-spec/native"), Authorize(Policy = "NativeGuest")]
    public record NativeCommand { public void Handle(Probe probe) => probe.Commands++; }

    [Path("/guest-spec/asp"), Microsoft.AspNetCore.Authorization.Authorize(Policy = "AspGuest")]
    public record AspCommand { public void Handle(Probe probe) => probe.Commands++; }

    [Path("/guest-spec/both"), Authorize(Policy = "NativeGuest"), Microsoft.AspNetCore.Authorization.Authorize(Policy = "AspGuest")]
    public record BothCommand { public void Handle(Probe probe) => probe.Commands++; }

    [Path("/guest-spec/denied"), Authorize(Policy = "AspDenied")]
    public record DeniedCommand { public void Handle(Probe probe) => probe.Commands++; }

    [Path("/guest-spec/ordinary"), Authorize(Policy = "AspOrdinary")]
    public record OrdinaryCommand { public void Handle(Probe probe) => probe.Commands++; }

    [Path("/guest-spec/mixed"), Authorize(Policy = "NativeGuest"), Authorize(Policy = "NativeOrdinary")]
    public record MixedCommand { public void Handle(Probe probe) => probe.Commands++; }

    [Path("/guest-spec/roles"), Authorize(Policy = "NativeGuest", Roles = "Admin")]
    public record RoleCommand { public void Handle(Probe probe) => probe.Commands++; }

    [Path("/guest-spec/schemes"), Authorize(Policy = "NativeGuest", AuthenticationSchemes = "Missing")]
    public record SchemeCommand { public void Handle(Probe probe) => probe.Commands++; }

    [Path("/guest-spec/authenticated"), Authorize]
    public record AuthenticatedCommand { public void Handle(Probe probe) => probe.Commands++; }

    [Path("/guest-spec/unannotated")]
    public record UnannotatedCommand { public void Handle(Probe probe) => probe.Commands++; }

    public record GuestReadModel
    {
        [Path("/guest-query/native"), Authorize(Policy = "NativeGuest")]
        public static int Native(Probe probe) => ++probe.Queries;

        [Path("/guest-query/asp"), Microsoft.AspNetCore.Authorization.Authorize(Policy = "AspGuest")]
        public static int Asp(Probe probe) => ++probe.Queries;

        [Path("/guest-query/both"), Authorize(Policy = "NativeGuest"), Microsoft.AspNetCore.Authorization.Authorize(Policy = "AspGuest")]
        public static int Both(Probe probe) => ++probe.Queries;

        [Path("/guest-query/denied"), Authorize(Policy = "AspDenied")]
        public static int Denied(Probe probe) => ++probe.Queries;

        [Path("/guest-query/ordinary"), Authorize(Policy = "AspOrdinary")]
        public static int Ordinary(Probe probe) => ++probe.Queries;

        [Path("/guest-query/mixed"), Authorize(Policy = "NativeGuest"), Authorize(Policy = "NativeOrdinary")]
        public static int Mixed(Probe probe) => ++probe.Queries;

        [Path("/guest-query/roles"), Authorize(Policy = "NativeGuest", Roles = "Admin")]
        public static int Roles(Probe probe) => ++probe.Queries;

        [Path("/guest-query/schemes"), Authorize(Policy = "NativeGuest", AuthenticationSchemes = "Missing")]
        public static int Schemes(Probe probe) => ++probe.Queries;

        [Path("/guest-query/authenticated"), Authorize]
        public static int Authenticated(Probe probe) => ++probe.Queries;

        [Path("/guest-query/unannotated")]
        public static int Unannotated(Probe probe) => ++probe.Queries;
    }

    class MissingCredentials(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
    }
}
