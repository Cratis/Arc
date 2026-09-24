// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ControllerBased;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Arc.Tenancy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// xUnit collection fixture that starts a single Kestrel web application for all scenario tests
/// in the collection, avoiding per-test-class server startup cost (~300-600ms each).
/// The fixture is created once before the first test in the collection and disposed after the last.
/// </summary>
public sealed class SharedScenarioWebHostFixture : IAsyncLifetime
{
    static volatile SharedScenarioWebHostFixture? _current;

    /// <summary>
    /// Gets the active fixture instance. Available after <see cref="InitializeAsync"/> and before <see cref="DisposeAsync"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed before the fixture is initialized or after it is disposed.</exception>
    public static SharedScenarioWebHostFixture Current =>
        _current ?? throw new InvalidOperationException(
            "SharedScenarioWebHostFixture is not initialized. Ensure the test class is in [Collection(ScenarioCollectionDefinition.Name)].");

    IHost? _host;

    /// <summary>
    /// Gets the running web host.
    /// </summary>
    public IHost Host => _host!;

    /// <summary>
    /// Gets the base URL the server is listening on.
    /// </summary>
    public string ServerUrl { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _current = this;

        RegisterReadModels(typeof(SharedScenarioWebHostFixture).Assembly);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel(options =>
            options.Listen(IPAddress.Loopback, 0, o => o.Protocols = HttpProtocols.Http1AndHttp2));
        builder.Logging.ClearProviders();
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(SharedScenarioWebHostFixture).Assembly);
        builder.Services.AddRouting();
        builder.Services.AddSingleton<ScenarioHttpContextAccessor>();
        builder.Services.AddSingleton<IHttpContextAccessor>(services => services.GetRequiredService<ScenarioHttpContextAccessor>());
        builder.Services.AddAuthentication("Default")
            .AddScheme<AuthenticationSchemeOptions, ScenarioAuthentication>("Default", _ => { })
            .AddScheme<AuthenticationSchemeOptions, ScenarioAuthentication>("Special", _ => { })
            .AddScheme<AuthenticationSchemeOptions, ScenarioAuthentication>("Other", _ => { });
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("ActiveSubscription", policy => policy
                .AddAuthenticationSchemes("Special")
                .RequireClaim("membership", "active"))
            .AddPolicy("Audit", policy => policy.RequireClaim("permission", "audit"))
            .AddPolicy("OtherSubscription", policy => policy
                .AddAuthenticationSchemes("Other")
                .RequireClaim("membership", "active"));
        builder.AddCratisArc(options => options.Tenancy.ResolverType = TenantResolverType.Claim);
        builder.Services.Replace(ServiceDescriptor.Singleton<IAuthorizationPolicyProvider, ScenarioAuthorizationPolicyProvider>());
        builder.Services.AddScoped<TenantBoundService>();
        builder.Services.AddSingleton<TenantFlowObservations>();
        builder.Services.AddSingleton<TenantCommandObservations>();
        builder.Services.AddSingleton<SelectedEmissionObservations>();
        builder.Services.AddSingleton<SelectedStreamInterceptorObservations>();
        builder.Services.AddSingleton<SelectedQueryFilterObservations>();
        builder.Services.AddSingleton<SelectedAuthorizationQueryFilterObservations>();
        builder.Services.AddSingleton<PolicyGate>();
        builder.Services.AddScoped<ScopedPolicyProbe>();
        builder.Services.AddArcAuthorizationPolicy<GatedPolicy>("Gated");
        builder.Services.AddArcAuthorizationPolicy<NonCooperativePolicy>("IgnoringCancellation");
        builder.Services.AddSingleton<ObservableControllerQueriesState>();

        var app = builder.Build();
        app.UseDeveloperExceptionPage();
        app.UseWebSockets();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.Use(async (context, next) =>
        {
            if (context.Request.Headers.ContainsKey("X-Controlled-Server-Abort"))
            {
                context.RequestAborted = context.RequestServices.GetRequiredService<PolicyGate>().ServerAbortToken;
            }

            if (!context.Request.Headers.ContainsKey("X-PreResolve-Tenant"))
            {
                await next(context);
                return;
            }

            var existing = context.RequestServices.GetRequiredService<TenantBoundService>();
            context.Items["PreResolvedTenant"] = existing.Tenant.Value;
            context.Items["PreResolvedServiceId"] = existing.Id;
            var flowId = context.Request.Headers["X-Flow-Id"].ToString();
            var observations = context.RequestServices.GetRequiredService<TenantFlowObservations>();
            if (!string.IsNullOrEmpty(flowId))
            {
                observations.Begin(flowId, existing);
            }

            try
            {
                await next(context);
            }
            finally
            {
                if (!string.IsNullOrEmpty(flowId))
                {
                    observations.Complete(flowId, context.RequestServices.GetRequiredService<TenantBoundService>());
                }
            }
        });
        app.UseCratisArc();
        app.MapPost("/.cratis-test/explicit-scope", async context =>
        {
            var pipeline = context.RequestServices.GetRequiredService<ICommandPipeline>();
            var result = await pipeline.Execute(
                new for_Commands.ModelBound.PolicyProtectedCommand(),
                context.RequestServices,
                context.RequestAborted);
            await context.Response.WriteAsJsonAsync(new { result.IsAuthorized, result.AuthorizationFailureReason });
        });
        app.MapGet("/.cratis-test/explicit-query-scope", async context =>
        {
            var pipeline = context.RequestServices.GetRequiredService<IQueryPipeline>();
            var name = new FullyQualifiedQueryName($"{typeof(for_Queries.ModelBound.PolicyProtectedReadModel).FullName}.All");
            var result = await pipeline.Perform(name, QueryArguments.Empty, Paging.NotPaged, Sorting.None, context.RequestServices, context.RequestAborted);
            await context.Response.WriteAsJsonAsync(new { result.IsAuthorized, result.ExceptionMessages });
        });
        app.MapControllers();

        _host = app;
        await app.StartAsync();

        var server = app.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        ServerUrl = addresses?.Addresses.FirstOrDefault() ?? "http://localhost:5000";
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        _current = null;
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    static void RegisterReadModels(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsReadModel())
            {
                continue;
            }

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(m => m.IsValidQueryFor(type));

            foreach (var method in methods)
            {
                QueryMetadataRegistry.Register($"{type.FullName}.{method.Name}", type);
            }
        }
    }
}
