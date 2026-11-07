// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.EndpointGroups;
using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Path = Cratis.Arc.Queries.ModelBound.PathAttribute;

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

[Collection("UsesCurrentDirectory")]
public class when_an_operation_requires_a_scope
{
    [Fact]
    public async Task should_refuse_a_caller_whose_ceiling_lacks_the_scope_and_allow_one_that_holds_it()
    {
        var capabilityPort = FreePort();
        var builder = WebApplication.CreateBuilder();
        var defaultPort = FreePort();
        builder.WebHost.UseUrls($"http://127.0.0.1:{capabilityPort}", $"http://127.0.0.1:{defaultPort}");
        builder.AddCratisArc(options => options.EndpointGroups.Assign("capabilities", capabilityPort), configureBuilder: arc => arc.WithoutControllers().WithCapabilities());
        builder.Services.AddSingleton(new VerifiedCallerInstallation("https://resource.example", "test"));
        builder.Services.AddSingleton(new CallerResourceCatalog(new Dictionary<string, string> { ["https://resource.example"] = "audience" }));
        builder.Services.AddScoped<IVerifiedCredentialSource>(_ => Substitute.For<VerifiedCredentialSourceForSpecs>());
        var verified = Substitute.For<IVerifiedCallerAccessor>();
        builder.Services.AddScoped(_ => verified);
        var probe = new Probe();
        builder.Services.AddSingleton(probe);
        RegisterCommands(builder.Services);
        await using var app = builder.Build();
        app.UseCratisArc();
        await app.StartAsync(global::Xunit.TestContext.Current.CancellationToken);
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            verified.GetCurrent().Returns(new CallerResolution(Caller(), null));
            using var refused = await client.PostAsJsonAsync($"http://127.0.0.1:{capabilityPort}/scoped-spec", new { }, global::Xunit.TestContext.Current.CancellationToken);
            refused.StatusCode.ShouldEqual(HttpStatusCode.Forbidden);
            probe.ScopedCommands.ShouldEqual(0);

            verified.GetCurrent().Returns(new CallerResolution(Caller("content:write"), null));
            using var accepted = await client.PostAsJsonAsync($"http://127.0.0.1:{capabilityPort}/scoped-spec", new { }, global::Xunit.TestContext.Current.CancellationToken);
            accepted.StatusCode.ShouldEqual(HttpStatusCode.OK);
            probe.ScopedCommands.ShouldEqual(1);
        }
        finally
        {
            await app.StopAsync(global::Xunit.TestContext.Current.CancellationToken);
        }
    }

    static VerifiedCaller Caller(params string[] scopes) =>
        new("1", CallerPrincipalKind.Person, CallerCredentialKind.PersonalAccessToken, new("credential"), null, new("person"), null, null, null, null, null, "resource", null, null, null, "test", new HashSet<string>(scopes), new HashSet<string>(), null);

    static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    static void RegisterCommands(IServiceCollection services)
    {
        var handlers = new[] { typeof(ScopedCommand) }
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
    }

    public class Probe
    {
        public int ScopedCommands { get; set; }
    }

    [Path("/scoped-spec"), EndpointGroup("capabilities"), RequiresScope("content:write")]
    public record ScopedCommand
    {
        public void Handle(Probe probe) => probe.ScopedCommands++;
    }
}
