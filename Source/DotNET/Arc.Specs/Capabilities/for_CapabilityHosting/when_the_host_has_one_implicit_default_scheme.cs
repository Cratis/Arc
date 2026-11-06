// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.EndpointGroups;
using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Path = Cratis.Arc.Queries.ModelBound.PathAttribute;

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

[Collection("UsesCurrentDirectory")]
public class when_the_host_has_one_implicit_default_scheme
{
    const string ProductScheme = "product";
    const string SubjectHeader = "X-Product-Subject";

    [Fact]
    public async Task should_keep_authenticating_the_default_port_and_refuse_a_conflicting_subject_on_the_capability_port()
    {
        var capabilityPort = FreePort();
        var defaultPort = FreePort();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{capabilityPort}", $"http://127.0.0.1:{defaultPort}");
        builder.AddCratisArc(options => options.EndpointGroups.Assign("capabilities", capabilityPort), configureBuilder: arc => arc.WithoutControllers().WithCapabilities());
        builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, ProductHandler>(ProductScheme, null);
        builder.Services.AddSingleton(new VerifiedCallerInstallation("https://resource.example", "test"));
        builder.Services.AddSingleton(new CallerResourceCatalog(new Dictionary<string, string> { ["https://resource.example"] = "audience" }));
        builder.Services.AddScoped(_ => Substitute.For<IVerifiedCredentialSource>());
        var verified = Substitute.For<IVerifiedCallerAccessor>();
        var person = new VerifiedCaller("1", CallerPrincipalKind.Person, CallerCredentialKind.PersonalAccessToken, new("credential"), null, new("person"), null, null, null, null, null, "resource", null, null, null, "test", new HashSet<string>(), new HashSet<string>(), null);
        verified.GetCurrent().Returns(new CallerResolution(person, null));
        builder.Services.AddScoped(_ => verified);
        var probe = new Probe();
        builder.Services.AddSingleton(probe);
        RegisterCommands(builder.Services);
        await using var app = builder.Build();
        app.UseCratisArc();
        await app.StartAsync(global::Xunit.TestContext.Current.CancellationToken);
        try
        {
            var defaultScheme = await app.Services.GetRequiredService<IAuthenticationSchemeProvider>().GetDefaultAuthenticateSchemeAsync();
            defaultScheme.ShouldNotBeNull();
            defaultScheme.Name.ShouldEqual(ProductScheme);

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            using var ordinary = await Post(client, $"http://127.0.0.1:{defaultPort}/ordinary-spec", "alice");
            ordinary.StatusCode.ShouldEqual(HttpStatusCode.OK);
            probe.OrdinarySubject.ShouldEqual("alice");

            using var conflicting = await Post(client, $"http://127.0.0.1:{capabilityPort}/capability-spec", "someone-else");
            conflicting.StatusCode.ShouldEqual(HttpStatusCode.Forbidden);
            probe.CapabilityCommands.ShouldEqual(0);

            using var matching = await Post(client, $"http://127.0.0.1:{capabilityPort}/capability-spec", "person");
            matching.StatusCode.ShouldEqual(HttpStatusCode.OK);
            probe.CapabilityCommands.ShouldEqual(1);
        }
        finally
        {
            await app.StopAsync(global::Xunit.TestContext.Current.CancellationToken);
        }
    }

    static async Task<HttpResponseMessage> Post(HttpClient client, string url, string subject)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(new { }) };
        request.Headers.Add(SubjectHeader, subject);
        return await client.SendAsync(request, global::Xunit.TestContext.Current.CancellationToken);
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
        public string? OrdinarySubject { get; set; }
    }

    sealed class ProductHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(Request.Headers.TryGetValue(SubjectHeader, out var subject)
                ? AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", subject.ToString())], ProductScheme)), ProductScheme))
                : AuthenticateResult.NoResult());
    }

    [Path("/capability-spec"), EndpointGroup("capabilities")]
    public record CapabilityCommand
    {
        public void Handle(Probe probe) => probe.CapabilityCommands++;
    }

    [Path("/ordinary-spec")]
    public record OrdinaryCommand
    {
        public void Handle(Probe probe, ICurrentPrincipalAccessor principals) =>
            probe.OrdinarySubject = principals.Current?.FindFirst("sub")?.Value;
    }
}
