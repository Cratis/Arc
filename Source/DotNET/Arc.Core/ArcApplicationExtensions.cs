// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
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
    /// <param name="prefixes">Optional HTTP prefixes to listen on. Defaults to http://localhost:5000/.</param>
    /// <returns>The <see cref="ArcApplication"/> for continuation.</returns>
    public static ArcApplication UseCratisArc(this ArcApplication app, params string[] prefixes)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        var endpointMapper = new HttpListenerEndpointMapper(prefixes);
#pragma warning restore CA2000 // Dispose objects before losing scope
        app.SetEndpointMapper(endpointMapper);

        app.AddStartupAction(serviceProvider =>
        {
            endpointMapper.Start(serviceProvider);
            return Task.CompletedTask;
        });

        var applicationLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        applicationLifetime.ApplicationStopping.Register(() =>
        {
            endpointMapper.StopAsync().GetAwaiter().GetResult();
            endpointMapper.Dispose();
        });

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
