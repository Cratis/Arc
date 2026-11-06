// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Tenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

[Collection("UsesCurrentDirectory")]
public class when_a_signed_browser_caller_requires_membership
{
    const string Subject = "8b167ad2-0f89-4c78-9d42-6d4580495d32";
    const string Issuer = "https://identity.example.test";
    const string Route = "/signed-membership-probe";

    [Fact]
    public async Task should_check_membership_as_the_verified_subject()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var control = await Send(key, null);
        control.Status.ShouldEqual(HttpStatusCode.OK);
        var membership = new ProductMembership();
        var result = await Send(key, membership);
        membership.Calls.ShouldBeGreaterThan(0);
        membership.Subject.ShouldEqual(Subject);
        membership.Tenant.ShouldEqual("tenant-test");
        result.Status.ShouldEqual(HttpStatusCode.OK);
        result.Invoked.ShouldBeTrue();
    }

    [Fact]
    public async Task should_refuse_without_invoking_the_operation_when_membership_denies()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var membership = new ProductMembership { Deny = true };
        var result = await Send(key, membership);
        membership.Calls.ShouldBeGreaterThan(0);
        result.Status.ShouldEqual(HttpStatusCode.Forbidden);
        result.Invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task should_refuse_when_the_resolved_tenant_differs_from_the_signed_tenant()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var membership = new ProductMembership();
        var result = await Send(key, membership, resolvedTenant: "tenant-other");
        result.Status.ShouldNotEqual(HttpStatusCode.OK);
        result.Invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task should_keep_default_port_tenant_resolution_unchanged()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var membership = new ProductMembership { AllowAnyone = true };
        var result = await Send(key, membership, defaultPort: true);
        result.Status.ShouldEqual(HttpStatusCode.OK);
        result.Body.ShouldEqual("tenant-test");
        membership.Calls.ShouldBeGreaterThan(0);
        membership.Subject.ShouldBeNull();
    }

    static async Task<Outcome> Send(ECDsa key, ProductMembership? membership, string? resolvedTenant = null, bool defaultPort = false)
    {
        var capabilityPort = FreePort();
        var otherPort = FreePort();
        var invoked = false;
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{capabilityPort}", $"http://127.0.0.1:{otherPort}");
        builder.AddCratisArc(
            options =>
            {
                options.EndpointGroups.Assign("capabilities", capabilityPort);
                if (resolvedTenant is not null) options.Tenancy.HttpHeader = "x-product-tenant";
            },
            configureBuilder: arc => arc
                .WithoutControllers().WithCapabilities().WithVerifiedCallers(callers => callers
                    .ForResource("https://resource.example", "example-api", "test").UsePersonDirectory<PersonDirectory>()
                    .UseSignedForwardedIdentityV2(v2 => v2.WithRealmIssuer(Issuer).AddPublicKey("test", key.ExportSubjectPublicKeyInfoPem()).UseProcessLocalNonceStore())));
        if (membership is not null) builder.Services.AddSingleton<ITenantMembership>(membership);
        await using var app = builder.Build();
        app.UseCratisArc();
        app.MapPost(Route, (ITenantIdAccessor tenants) =>
        {
            invoked = true;
            return Results.Text(tenants.Current.Value);
        });
        await app.StartAsync(global::Xunit.TestContext.Current.CancellationToken);
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            using var request = SignedRequest(key, defaultPort ? otherPort : capabilityPort);
            if (resolvedTenant is not null) request.Headers.Add("x-product-tenant", resolvedTenant);
            using var response = await client.SendAsync(request, global::Xunit.TestContext.Current.CancellationToken);
            return new(response.StatusCode, invoked, await response.Content.ReadAsStringAsync(global::Xunit.TestContext.Current.CancellationToken));
        }
        finally
        {
            await app.StopAsync(global::Xunit.TestContext.Current.CancellationToken);
        }
    }

    static HttpRequestMessage SignedRequest(ECDsa key, int port)
    {
        var principal = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new
        {
            identityProvider = "https://login.example.test", userId = Subject, userDetails = "Test User", userRoles = new[] { "user" }, claims = Array.Empty<object>()
        }));
        var headers = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["x-ms-client-principal"] = principal,
            ["x-ms-client-principal-id"] = Subject,
            ["x-ms-client-principal-name"] = "Test User",
            ["x-cratis-credential-kind"] = "browser-session",
            ["x-cratis-credential-id"] = "credential-test",
            ["x-cratis-issuer"] = Issuer,
            ["x-cratis-subject"] = Subject,
            ["x-cratis-idp"] = "https://login.example.test",
            ["x-cratis-auth-time"] = "1699999900",
            ["x-cratis-acr"] = "basic",
            ["x-cratis-sender-binding"] = "none",
            ["x-cratis-token-scope"] = "content:write",
            ["x-cratis-tenant-id"] = "tenant-test"
        };
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var nonce = Url(RandomNumberGenerator.GetBytes(16));
        var input = new StringBuilder("Cratis.AuthProxy.ForwardedIdentity.v2\n");
        foreach (var value in new[] { "example-api", "test", timestamp, nonce, "POST", Route, headers.Count.ToString(CultureInfo.InvariantCulture) }) Frame(input, value);
        foreach (var header in headers)
        {
            Frame(input, header.Key);
            Frame(input, header.Value);
        }
        var signature = Url(key.SignData(Encoding.UTF8.GetBytes(input.ToString()), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        var request = new HttpRequestMessage(HttpMethod.Post, $"http://127.0.0.1:{port}{Route}");
        foreach (var header in headers) request.Headers.Add(header.Key, header.Value);
        request.Headers.Add("x-cratis-forwarded-identity-signature", $"v2.test.{timestamp}.{nonce}.{signature}");
        return request;
    }

    static void Frame(StringBuilder input, string value) => input.Append(Encoding.UTF8.GetByteCount(value).ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append('\n');
    static string Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    sealed record Outcome(HttpStatusCode Status, bool Invoked, string Body);

    public class PersonDirectory : ICallerPersonDirectory
    {
        public Task<CallerPersonFound> Find(CallerAccountKey accountKey, CancellationToken cancellationToken) => Task.FromResult(CallerPersonFound.Found(new(Subject)));
    }

    public class ProductMembership : ITenantMembership
    {
        public int Calls { get; private set; }
        public string? Subject { get; private set; }
        public string? Tenant { get; private set; }
        public bool Deny { get; init; }
        public bool AllowAnyone { get; init; }
        public bool IsMember(ClaimsPrincipal principal, TenantId tenantId)
        {
            Calls++;
            Subject = principal.FindFirst("sub")?.Value;
            Tenant = tenantId.Value;
            if (Deny) return false;
            return tenantId.Value == "tenant-test" && (AllowAnyone || Subject == when_a_signed_browser_caller_requires_membership.Subject);
        }
    }
}
