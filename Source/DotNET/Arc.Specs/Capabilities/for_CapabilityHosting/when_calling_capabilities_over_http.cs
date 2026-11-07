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
public class when_calling_capabilities_over_http
{
    [Fact]
    public async Task should_require_a_caller_only_on_the_capability_port()
    {
        var capabilityPort = FreePort();
        var defaultPort = FreePort();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{capabilityPort}", $"http://127.0.0.1:{defaultPort}");
        builder.AddCratisArc(options => options.EndpointGroups.Assign("capabilities", capabilityPort), configureBuilder: arc => arc.WithoutControllers().WithCapabilities());
        builder.Services.AddSingleton(new VerifiedCallerInstallation("https://resource.example", "test"));
        builder.Services.AddSingleton(new CallerResourceCatalog(new Dictionary<string, string> { ["https://resource.example"] = "audience" }));
        builder.Services.AddScoped<IVerifiedCredentialSource>(_ => Substitute.For<VerifiedCredentialSourceForSpecs>());
        var verified = Substitute.For<IVerifiedCallerAccessor>();
        verified.GetCurrent().Returns(new CallerResolution(null, CallerRefusal.NoCredential));
        builder.Services.AddScoped(_ => verified);
        var probe = new Probe();
        builder.Services.AddSingleton(probe);
        RegisterCommands(builder.Services);
        await using var app = builder.Build();
        app.UseCratisArc();
        await app.StartAsync(global::Xunit.TestContext.Current.CancellationToken);
        try
        {
            app.Services.GetServices<ICapabilityCallers>().Count().ShouldEqual(1);
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            using var refused = await client.PostAsJsonAsync($"http://127.0.0.1:{capabilityPort}/capability-spec", new { }, global::Xunit.TestContext.Current.CancellationToken);
            refused.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
            refused.Content.Headers.ContentType!.MediaType.ShouldEqual("application/problem+json");
            refused.Headers.WwwAuthenticate.Single().Scheme.ShouldEqual("Bearer");
            probe.CapabilityCommands.ShouldEqual(0);
            var person = new VerifiedCaller("1", CallerPrincipalKind.Person, CallerCredentialKind.PersonalAccessToken, new("credential"), null, new("person"), null, null, null, null, null, "resource", null, null, null, "test", new HashSet<string>(), new HashSet<string>(), null);
            verified.GetCurrent().Returns(new CallerResolution(person, null));
            using var accepted = await client.PostAsJsonAsync($"http://127.0.0.1:{capabilityPort}/capability-spec", new { }, global::Xunit.TestContext.Current.CancellationToken);
            accepted.StatusCode.ShouldEqual(HttpStatusCode.OK);
            probe.CapabilityCommands.ShouldEqual(1);
            probe.Caller!.Person.ShouldEqual("person");
            probe.Caller.EndpointGroup.ShouldEqual("capabilities");
            probe.Subject.ShouldEqual("person");
            verified.ClearReceivedCalls();
            using var wrongPort = await client.PostAsJsonAsync($"http://127.0.0.1:{defaultPort}/capability-spec", new { }, global::Xunit.TestContext.Current.CancellationToken);
            wrongPort.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
            using var ordinary = await client.PostAsJsonAsync($"http://127.0.0.1:{defaultPort}/ordinary-spec", new { }, global::Xunit.TestContext.Current.CancellationToken);
            ordinary.StatusCode.ShouldEqual(HttpStatusCode.OK);
            probe.OrdinaryCommands.ShouldEqual(1);
            probe.Caller.ShouldBeNull();
            verified.ReceivedCalls().ShouldBeEmpty();
        }
        finally
        {
            await app.StopAsync(global::Xunit.TestContext.Current.CancellationToken);
        }
    }

    static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    static void RegisterCommands(IServiceCollection services)
    {
        var handlers = new[] { typeof(CapabilityCommand), typeof(OrdinaryCommand) }
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
        public int CapabilityCommands { get; set; }
        public int OrdinaryCommands { get; set; }
        public CapabilityCaller? Caller { get; set; }
        public string? Subject { get; set; }
    }

    [Path("/capability-spec"), EndpointGroup("capabilities")]
    public record CapabilityCommand
    {
        public void Handle(Probe probe, ICapabilityCallers callers, ICurrentPrincipalAccessor principals)
        {
            probe.CapabilityCommands++;
            probe.Caller = callers.Current;
            probe.Subject = principals.Current?.FindFirst("sub")?.Value;
        }
    }

    [Path("/ordinary-spec")]
    public record OrdinaryCommand
    {
        public void Handle(Probe probe, ICapabilityCallers callers)
        {
            probe.OrdinaryCommands++;
            probe.Caller = callers.Current;
        }
    }
}
