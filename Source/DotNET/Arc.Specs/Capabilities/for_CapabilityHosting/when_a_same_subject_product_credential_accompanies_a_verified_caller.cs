// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

[Collection("UsesCurrentDirectory")]
public class when_a_same_subject_product_credential_accompanies_a_verified_caller
{
    const string ProductScheme = "product";
    const string SubjectHeader = "X-Product-Subject";

    [Fact]
    public async Task should_still_hold_a_controller_action_to_the_scope_ceiling_of_the_verified_caller()
    {
        var capabilityPort = FreePort();
        var defaultPort = FreePort();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{capabilityPort}", $"http://127.0.0.1:{defaultPort}");
        builder.AddCratisArc(options => options.EndpointGroups.Assign("capabilities", capabilityPort), configureBuilder: arc => arc.WithCapabilities());
        builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, ProductHandler>(ProductScheme, null);
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
            var url = $"http://127.0.0.1:{capabilityPort}/scoped-controller-spec";
            verified.GetCurrent().Returns(new CallerResolution(Caller(), null));

            using var getRefused = await Send(client, HttpMethod.Get, url);
            using var postRefused = await Send(client, HttpMethod.Post, url);
            getRefused.StatusCode.ShouldEqual(HttpStatusCode.Forbidden);
            postRefused.StatusCode.ShouldEqual(HttpStatusCode.Forbidden);
            probe.Invocations.ShouldEqual(0);

            verified.GetCurrent().Returns(new CallerResolution(Caller("content:write"), null));
            using var getAccepted = await Send(client, HttpMethod.Get, url);
            using var postAccepted = await Send(client, HttpMethod.Post, url);
            getAccepted.StatusCode.ShouldEqual(HttpStatusCode.OK);
            postAccepted.StatusCode.ShouldEqual(HttpStatusCode.OK);
            probe.Invocations.ShouldEqual(2);
        }
        finally
        {
            await app.StopAsync(global::Xunit.TestContext.Current.CancellationToken);
        }
    }

    static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string url)
    {
        using var request = new HttpRequestMessage(method, url);
        if (method == HttpMethod.Post)
        {
            request.Content = JsonContent.Create(new { });
        }

        request.Headers.Add(SubjectHeader, "person");
        return await client.SendAsync(request, global::Xunit.TestContext.Current.CancellationToken);
    }

    static VerifiedCaller Caller(params string[] scopes) =>
        new("1", CallerPrincipalKind.Person, CallerCredentialKind.PersonalAccessToken, new("credential"), null, new("person"), null, null, null, null, null, "resource", null, null, null, "test", new HashSet<string>(scopes), new HashSet<string>(), null);

    static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
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
}
