// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc;
using Cratis.Arc.AspNetCore.EndpointGroups;
using Cratis.Arc.Capabilities;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp;
using Cratis.Arc.PersonalAccessTokens;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace OAuthPatCallerAcceptance;

/// <summary>An external application using only public Arc APIs for covered OAuth and PAT capability callers.</summary>
public static class Program
{
    /// <summary>Explains how to run the isolated acceptance host.</summary>
    public static void Main() => Console.WriteLine("Run OAuthPatCallerAcceptance.Specs for the isolated MCP and REST acceptance host. No production credentials are configured.");

    /// <summary>Builds an isolated host. Synthetic authority data is never production authentication.</summary>
    /// <param name="settings">Listener, trust and cache settings.</param>
    /// <param name="store">Independent synthetic identity and credential authorities.</param>
    /// <returns>The application, not yet started.</returns>
    public static WebApplication Build(SampleSettings settings, SampleIdentityStore store)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(Program).Assembly.FullName });
        builder.WebHost.ConfigureKestrel(server =>
        {
            server.Listen(IPAddress.Loopback, settings.DefaultPort);
            server.Listen(IPAddress.Loopback, settings.CapabilityPort, listener => listener.UseHttps(https =>
            {
                https.ServerCertificate = settings.ServerCertificate;
                https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;

                // Transport is isolated; Arc independently checks the configured peer CA and SAN.
                https.ClientCertificateValidation = (_, _, _) => true;
            }));
        });
        builder.Services.AddSingleton(store);
        builder.Services.AddScoped<Cratis.Arc.Tenancy.ITenantMembership, PersonMembership>();
        builder.Services.AddAuthentication();
        builder.Services.AddScoped<IPatAuthority, PatAuthority>();
        builder.Services.AddScoped<IPatRecoveryEpoch, PatRecoveryEpoch>();
        builder.Services.AddScoped<IPatPolicy, PatPolicy>();
        builder.AddCratisArc(
            options =>
            {
                options.EndpointGroups.Assign("capabilities", settings.CapabilityPort);
                options.Tenancy.HttpHeader = "X-Tenant-ID";
                options.ForwardedIdentity.ClientCertificate.Required = true;
                options.ForwardedIdentity.ClientCertificate.TrustedCertificateAuthoritiesPath = settings.TrustedCertificateAuthoritiesPath;
                options.ForwardedIdentity.ClientCertificate.AllowedDnsNames.Add("proxy.example.test");
            },
            arc => arc.WithoutControllers()
                .WithPersonalAccessTokens(new("direct") { AllowedScopes = ["read"] }, callers => ConfigureCallers(callers, settings))
                .WithCapabilities(capabilities =>
                {
                    capabilities.GatedEndpoints = new CapabilityEndpointSelector().ForRoutePattern("/rest").ForRoutePattern("/pat-only");
                    capabilities.AllowedPrincipalKinds.Clear();
                    capabilities.AllowedPrincipalKinds.Add(CapabilityPrincipalKind.Person);
                })
                .WithMcp(mcp => mcp.AddToolSource<ReadTool>().Configure(options =>
                {
                    options.Path = "/mcp";
                    options.Profile = settings.PatOnly ? McpProfile.PatOnly : McpProfile.Combined;
                    options.Resource = SampleSettings.Resource;
                    options.PublicOrigin = "https://api.example.test";
                    options.AuthorizationServers.Add(SampleSettings.Issuer);
                })));
        if (settings.ResolveBeforeGate) builder.Services.Insert(0, ServiceDescriptor.Singleton<Microsoft.AspNetCore.Hosting.IStartupFilter, ResolveCallerBeforeGate>());
        var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseCratisArc();
        var group = new EndpointGroupMetadata("capabilities");
        app.MapGet("/rest", Read).WithMetadata(group, AcceptsOAuthCapability.Instance);
        app.MapGet("/pat-only", Read).WithMetadata(group);

        // Browser issuance is outside the bearer capability gate; the PAT module refuses OAuth-to-PAT exchange.
        app.MapPost("/issue", Issue).WithMetadata(group);
        app.MapArcMcp();
        return app;
    }

    static void ConfigureCallers(VerifiedCallersBuilder callers, SampleSettings settings)
    {
        callers.ForResource(SampleSettings.Resource, SampleSettings.Audience, "acceptance")
            .UsePersonDirectory<PersonDirectory>().AllowPersonalAccessTokenCallers()
            .UseSignedForwardedIdentityV2(forwarding => forwarding
                .WithRealmIssuer(SampleSettings.Issuer)
                .WithTenantHeader("X-Tenant-ID")
                .AddPublicKey("sample-key", settings.PublicKeyPem)
                .UseProcessLocalNonceStore());
        if (settings.AllowOAuth) callers.AllowOAuthBearerCallers<OAuthVerifier>();
    }

    static async Task<IResult> Issue(IPersonalAccessTokens tokens, CancellationToken cancellationToken)
    {
        var issued = await tokens.Issue(TimeSpan.FromMinutes(5), ["read"], cancellationToken);
        using var token = issued.Token;
        return token is null ? Results.Problem(statusCode: StatusCodes.Status401Unauthorized, type: "about:blank") : Results.Json(new { credentialId = token.CredentialId.Value, accessToken = token.Reveal() });
    }

    static IResult Read(ICapabilityCallers callers, SampleIdentityStore store)
    {
        var caller = callers.Current!;
        if (!caller.Scopes.Contains("read")) return Results.StatusCode(StatusCodes.Status403Forbidden);
        store.Dispatched();
        return Results.Text($"{caller.Person}|{caller.CredentialId}|{caller.Tenant}");
    }
}
