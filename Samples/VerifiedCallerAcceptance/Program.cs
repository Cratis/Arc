// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Tenancy;
using Microsoft.AspNetCore.Authentication;

namespace VerifiedCallerAcceptance;

/// <summary>
/// Hosts the signed-browser verified caller acceptance sample.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs the sample on ports 8081 (capabilities) and 8080 (default).
    /// </summary>
    /// <param name="args">The path to a public-only P-256 PEM file.</param>
    /// <returns>The running host task.</returns>
    public static async Task Main(string[] args)
    {
        var publicKey = await File.ReadAllTextAsync(args[0]);
        await using var app = Build([], new(8081, 8080, publicKey));
        await app.RunAsync();
    }

    /// <summary>
    /// Builds a host using only public Arc installation and product authorization seams.
    /// </summary>
    /// <param name="args">Host arguments.</param>
    /// <param name="settings">Explicit listener and verification configuration.</param>
    /// <returns>The configured application, not yet started.</returns>
    public static WebApplication Build(string[] args, VerifiedCallerAcceptanceSettings settings)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls($"http://127.0.0.1:{settings.CapabilityPort}", $"http://127.0.0.1:{settings.DefaultPort}");
        builder.AddCratisArc(
            options =>
            {
                options.EndpointGroups.Assign("capabilities", settings.CapabilityPort);
                options.Tenancy.HttpHeader = VerifiedCallerAcceptanceSettings.TenantHeader;
            },
            arc => arc.WithoutControllers().WithCapabilities().WithVerifiedCallers(callers => callers
                .ForResource(VerifiedCallerAcceptanceSettings.Resource, VerifiedCallerAcceptanceSettings.Audience, "sample")
                .UsePersonDirectory<PersonDirectory>()
                .UseSignedForwardedIdentityV2(v2 => v2
                    .WithRealmIssuer(VerifiedCallerAcceptanceSettings.RealmIssuer)
                    .WithTenantHeader(VerifiedCallerAcceptanceSettings.TenantHeader)
                    .AddPublicKey(settings.KeyId, settings.PublicKeyPem)
                    .UseProcessLocalNonceStore())));
        builder.Services.AddSingleton<InvocationCounts>();
        builder.Services.AddTransient<IClaimsTransformation, ProductRoles>();
        builder.Services.AddSingleton<ITenantMembership, ProductMembership>();
        builder.Services.AddAuthentication("ProductHeader").AddScheme<AuthenticationSchemeOptions, ProductHeader>("ProductHeader", null);
        var app = builder.Build();
        app.UseAuthentication();
        app.UseCratisArc();
        return app;
    }
}
