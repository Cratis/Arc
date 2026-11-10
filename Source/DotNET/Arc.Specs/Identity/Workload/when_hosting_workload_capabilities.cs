// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Capabilities;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Tenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Identity.Workload;

[Collection("UsesCurrentDirectory")]
public class when_hosting_workload_capabilities : Specification
{
    [Fact]
    public async Task should_enforce_the_tenantless_workload_lane_without_changing_the_default_port()
    {
        using var certificates = new for_ForwardedIdentityHop.given.certificates();
        var port = FreePort();
        var otherPort = FreePort();
        var kubernetes = new given.kubernetes_client();
        var membership = new given.workload_membership();
        var invoked = 0;
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(server =>
        {
            server.Listen(IPAddress.Loopback, port);
            server.Listen(IPAddress.Loopback, otherPort);
        });
        builder.Services.AddSingleton<IStartupFilter, given.workload_principal_filter>();
        builder.AddCratisArc(
            options =>
            {
                options.EndpointGroups.Assign("capabilities", port);
                options.Tenancy.HttpHeader = "x-tenant";
            },
            configureBuilder: arc => Configure(arc, certificates.Bundle));
        builder.Services.Replace(ServiceDescriptor.Singleton<IWorkloadTokenReviewClient>(kubernetes));
        builder.Services.AddSingleton<ITenantMembership>(membership);
        await using var app = builder.Build();
        app.UseCratisArc();
        app.MapPost("/probe", async (HttpContext context, IVerifiedCallerAccessor callers) =>
        {
            invoked++;
            return await Probe(context, callers);
        });
        app.MapGet("/outside", (HttpContext context) => Results.Text(context.User.FindFirst(ClaimTypes.Role)?.Value ?? "missing"));
        await app.StartAsync(global::Xunit.TestContext.Current.CancellationToken);
        try
        {
            using var client = new HttpClient { BaseAddress = new($"http://127.0.0.1:{port}"), Timeout = TimeSpan.FromSeconds(10) };
            await Exercise(client, kubernetes, membership, () => invoked);
            using var outside = await client.GetAsync($"http://127.0.0.1:{otherPort}/outside", global::Xunit.TestContext.Current.CancellationToken);
            outside.StatusCode.ShouldEqual(HttpStatusCode.OK);
            (await outside.Content.ReadAsStringAsync(global::Xunit.TestContext.Current.CancellationToken)).ShouldEqual("administrator");
        }
        finally
        {
            await app.StopAsync(global::Xunit.TestContext.Current.CancellationToken);
        }
    }

    static async Task<IResult> Probe(HttpContext context, IVerifiedCallerAccessor callers)
    {
        var first = await callers.GetCurrent();
        var second = await callers.GetCurrent();
        ReferenceEquals(first.Caller, second.Caller).ShouldBeTrue();
        first.Caller!.PrincipalKind.ShouldEqual(CallerPrincipalKind.ClusterWorkload);
        first.Caller.Tenant.ShouldBeNull();
        first.Caller.Person.ShouldBeNull();
        first.Caller.Organisation.ShouldBeNull();
        first.Caller.Actor.ShouldBeNull();
        first.Caller.GrantId.ShouldBeNull();
        first.Caller.Scopes.ShouldContainOnly("jobs.read");
        first.Caller.Permissions.ShouldContainOnly("jobs.run");
        context.User.FindFirst(ClaimTypes.Role).ShouldBeNull();
        context.User.FindFirst("email").ShouldBeNull();
        var raw = new AspNetCoreHttpRequestContext(context);
        raw.RawTarget.ShouldEqual("/probe");
        return Results.Ok();
    }

    static async Task Exercise(HttpClient client, given.kubernetes_client kubernetes, given.workload_membership membership, Func<int> invoked)
    {
        using var valid = Request(Token());
        valid.Headers.Add("x-tenant", "unsigned-tenant");
        valid.Headers.Add("x-forwarded-user", "unsigned-person");
        using var response = await client.SendAsync(valid, global::Xunit.TestContext.Current.CancellationToken);
        response.StatusCode.ShouldEqual(HttpStatusCode.OK);
        invoked().ShouldEqual(1);
        membership.Calls.ShouldEqual(0);
        kubernetes.Reviews.ShouldEqual(1);
        await RefusesAmbiguousCredentials(client);
        kubernetes.Reviews.ShouldEqual(1);
        invoked().ShouldEqual(1);
        kubernetes.Accept = false;
        using var refused = Request(Token(301));
        using var refusal = await client.SendAsync(refused, global::Xunit.TestContext.Current.CancellationToken);
        refusal.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
        invoked().ShouldEqual(1);
    }

    static void Configure(IArcBuilder arc, string fixturePath) => arc.WithoutControllers().WithCapabilities(options =>
    {
        options.AllowedPrincipalKinds.Clear();
        options.AllowedPrincipalKinds.Add(CapabilityPrincipalKind.ClusterWorkload);
    }).WithVerifiedCallers(callers => callers.ForResource("https://resource.example", "workload-audience", "test")
        .UseWorkloadCallers(workload => workload.WithOptions(new()
        {
            ReviewerTokenPath = fixturePath,
            CaPath = fixturePath,
            Allowlist = new([new("jobs", "worker", "sa-uid", "https://resource.example", "test", new("service-fixture"), new HashSet<string> { "jobs.read" }, new HashSet<string> { "jobs.run" })])
        })));

    static async Task RefusesAmbiguousCredentials(HttpClient client)
    {
        var requests = new[]
        {
            (new HttpRequestMessage(HttpMethod.Post, "/probe"), HttpStatusCode.Unauthorized),
            (Request("fixture_pat_invalid"), HttpStatusCode.Unauthorized),
            (Request(Token(), cookie: true), HttpStatusCode.BadRequest),
            (Request(Token(), duplicate: true), HttpStatusCode.BadRequest),
            (Request(Token(), path: "/probe?access_token=fixture"), HttpStatusCode.BadRequest),
            (Request(Token(), scheme: "DPoP"), HttpStatusCode.BadRequest)
        };
        foreach (var (request, expected) in requests)
        {
            using (request)
            using (var response = await client.SendAsync(request, global::Xunit.TestContext.Current.CancellationToken))
            {
                response.StatusCode.ShouldEqual(expected);
            }
        }
    }

    static HttpRequestMessage Request(string token, string path = "/probe", string scheme = "Bearer", bool cookie = false, bool duplicate = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.TryAddWithoutValidation("Authorization", duplicate ? [$"{scheme} {token}", $"{scheme} {token}"] : new[] { $"{scheme} {token}" });
        request.Headers.Add("X-Workload", "untrusted-fixture");
        if (cookie) request.Headers.Add("Cookie", ".AspNetCore.Cookies=fixture");
        return request;
    }

    static string Token(int lifetime = 300)
    {
        var body = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { exp = DateTimeOffset.UtcNow.AddSeconds(lifetime).ToUnixTimeSeconds() }))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"eyJhbGciOiJSUzI1NiJ9.{body}.fixture-signature";
    }

    static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
