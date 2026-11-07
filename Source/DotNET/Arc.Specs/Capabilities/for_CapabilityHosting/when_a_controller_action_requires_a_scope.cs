// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

[Collection("UsesCurrentDirectory")]
public class when_a_controller_action_requires_a_scope
{
    [Fact]
    public async Task should_refuse_an_under_scoped_caller_without_running_the_action_and_allow_one_that_holds_the_scope()
    {
        var capabilityPort = FreePort();
        var defaultPort = FreePort();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{capabilityPort}", $"http://127.0.0.1:{defaultPort}");
        builder.AddCratisArc(options => options.EndpointGroups.Assign("capabilities", capabilityPort), configureBuilder: arc => arc.WithCapabilities());
        builder.Services.AddSingleton(new VerifiedCallerInstallation("https://resource.example", "test"));
        builder.Services.AddSingleton(new CallerResourceCatalog(new Dictionary<string, string> { ["https://resource.example"] = "audience" }));
        builder.Services.AddScoped<IVerifiedCredentialSource>(_ => Substitute.For<VerifiedCredentialSourceForSpecs>());
        var verified = Substitute.For<IVerifiedCallerAccessor>();
        builder.Services.AddScoped(_ => verified);
        builder.Services.AddControllers().ConfigureApplicationPartManager(parts => parts.FeatureProviders.Add(new ScopedProbeControllerFeature()));
        var probe = new ScopedControllerProbe();
        builder.Services.AddSingleton(probe);
        await using var app = builder.Build();
        app.UseCratisArc();
        app.MapControllers();
        await app.StartAsync(global::Xunit.TestContext.Current.CancellationToken);
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var baseAddress = $"http://127.0.0.1:{capabilityPort}";
            verified.GetCurrent().Returns(new CallerResolution(Caller(), null));

            using var getRefused = await client.GetAsync($"{baseAddress}/scoped-controller-spec", global::Xunit.TestContext.Current.CancellationToken);
            using var postRefused = await client.PostAsJsonAsync($"{baseAddress}/scoped-controller-spec", new { }, global::Xunit.TestContext.Current.CancellationToken);
            getRefused.StatusCode.ShouldEqual(HttpStatusCode.Forbidden);
            postRefused.StatusCode.ShouldEqual(HttpStatusCode.Forbidden);
            probe.Invocations.ShouldEqual(0);

            verified.GetCurrent().Returns(new CallerResolution(Caller("content:write"), null));
            using var getAccepted = await client.GetAsync($"{baseAddress}/scoped-controller-spec", global::Xunit.TestContext.Current.CancellationToken);
            using var postAccepted = await client.PostAsJsonAsync($"{baseAddress}/scoped-controller-spec", new { }, global::Xunit.TestContext.Current.CancellationToken);
            getAccepted.StatusCode.ShouldEqual(HttpStatusCode.OK);
            postAccepted.StatusCode.ShouldEqual(HttpStatusCode.OK);
            probe.Invocations.ShouldEqual(2);
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
}
