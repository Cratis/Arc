// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Http;
using Cratis.Arc.Identity;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc;

/// <summary>
/// Extension methods for <see cref="ArcApplication"/>.
/// </summary>
public static class ArcApplicationExtensions
{
    static HttpListenerEndpointMapper? _endpointMapper;

    /// <summary>
    /// Configures Cratis Arc middleware and endpoints.
    /// </summary>
    /// <param name="app">The <see cref="ArcApplication"/>.</param>
    /// <returns>The <see cref="ArcApplication"/> for continuation.</returns>
    public static ArcApplication UseCratisArc(this ArcApplication app)
    {
        var prefixes = GetHttpPrefixes(app);

        Internals.ServiceProvider = app.Services;
        Internals.EndpointMapper = app.EndpointMapper!;

#pragma warning disable CA2000 // Dispose objects before losing scope
        var logger = app.Services.GetRequiredService<ILogger<HttpListenerEndpointMapper>>();
        _endpointMapper = new HttpListenerEndpointMapper(logger, [.. prefixes]);
#pragma warning restore CA2000 // Dispose objects before losing scope
        app.SetEndpointMapper(_endpointMapper);
        _endpointMapper.MapIdentityProviderEndpoint(app.Services);
        _endpointMapper.MapCommandEndpoints(app.Services);
        _endpointMapper.MapQueryEndpoints(app.Services);

        app.AddStartupAction(serviceProvider =>
        {
            _endpointMapper.Start(serviceProvider);
            return Task.CompletedTask;
        });

        var applicationLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        applicationLifetime.ApplicationStopping.Register(() =>
        {
            _endpointMapper.StopAsync().GetAwaiter().GetResult();
            _endpointMapper.Dispose();
        });

        return app;
    }

    /// <summary>
    /// Configures a base path for all endpoints being mapped.
    /// </summary>
    /// <param name="app">The <see cref="ArcApplication"/>.</param>
    /// <param name="pathBase">The base path to use for all endpoints.</param>
    /// <returns>The <see cref="ArcApplication"/> for continuation.</returns>
    public static ArcApplication UsePathBase(this ArcApplication app, string pathBase)
    {
        _endpointMapper?.SetPathBase(pathBase);
        return app;
    }

    /// <summary>
    /// Runs the application and blocks until shutdown.
    /// </summary>
    /// <param name="app">The <see cref="ArcApplication"/>.</param>
    /// <returns>A task representing the application lifetime.</returns>
    public static async Task RunAsync(this ArcApplication app)
    {
        await app.StartAsync();

        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        await WaitForShutdownAsync(lifetime);

        await app.StopAsync();
    }

    static Task WaitForShutdownAsync(IHostApplicationLifetime lifetime)
    {
        var tcs = new TaskCompletionSource();
        lifetime.ApplicationStopping.Register(() => tcs.TrySetResult());
        return tcs.Task;
    }

    static string[] GetHttpPrefixes(ArcApplication app) => app.Options.Hosting.ApplicationUrl
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(prefix => prefix.EndsWith('/') ? prefix : $"{prefix}/")
                .ToArray();
}
