// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Http;
using Cratis.Arc.Identity;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc;

/// <summary>
/// Extension methods for <see cref="ArcApplication"/>.
/// </summary>
public static class ArcApplicationExtensions
{
    /// <summary>
    /// Configures Cratis Arc middleware and endpoints.
    /// </summary>
    /// <param name="app">The <see cref="ArcApplication"/>.</param>
    /// <returns>The <see cref="ArcApplication"/> for continuation.</returns>
    public static ArcApplication UseCratisArc(this ArcApplication app)
    {
        if (app.IsCratisArcConfigured)
        {
            return app;
        }

        app.EndpointMapper.MapIdentityProviderEndpoint(app.Services);
        app.EndpointMapper.MapCommandEndpoints(app.Services);
        app.EndpointMapper.MapQueryEndpoints(app.Services);

        app.AddStartupAction(serviceProvider =>
        {
            if (app.EndpointMapper is HttpListenerEndpointMapper httpListenerEndpointMapper)
            {
                httpListenerEndpointMapper.Start(serviceProvider);
            }
            return Task.CompletedTask;
        });

        var applicationLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        applicationLifetime.ApplicationStopping.Register(() =>
        {
            if (app.EndpointMapper is HttpListenerEndpointMapper httpListenerEndpointMapper)
            {
                httpListenerEndpointMapper.Stop().GetAwaiter().GetResult();
                httpListenerEndpointMapper.Dispose();
            }
        });

        app.MarkCratisArcAsConfigured();

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
        if (app.EndpointMapper is HttpListenerEndpointMapper httpListenerEndpointMapper)
        {
            httpListenerEndpointMapper.SetPathBase(pathBase);
        }
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
}
