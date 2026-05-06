// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestApps.Authentication;

namespace TestApps.Maui.Services;

/// <summary>
/// Hosts <see cref="ArcApplication"/> as the HTTP backend for the MAUI app.
/// Arc's HttpListener binds to localhost:5001 and serves both the REST/WebSocket API
/// and static frontend assets from wwwroot/. The MAUI WebView navigates to that URL.
/// </summary>
public class ArcHostService(ILogger<ArcHostService> logger) : IAsyncDisposable
{
    const int Port = 5001;
    const string LoopbackHost = "http://127.0.0.1";
    const string LoopbackAlias = "http://localhost";

    ArcApplication? _app;

    /// <summary>
    /// Gets a value indicating whether the Arc backend is currently running.
    /// </summary>
    public bool IsRunning => _app is not null;

    /// <summary>
    /// Starts the Arc HTTP backend. Safe to call once — subsequent calls are no-ops.
    /// </summary>
    /// <param name="appDataDirectory">The platform application data directory.</param>
    public async Task StartAsync(string appDataDirectory)
    {
        if (_app is not null)
        {
            logger.AlreadyRunning();

            return;
        }

#if DEBUG
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
#endif

        logger.Starting(Port);

        var builder = ArcApplication.CreateBuilder([]);

        // Force the Shared assembly to load before Arc scans assemblies so that all
        // commands, queries, and read models are discovered by IImplementationsOf<T>.
        _ = typeof(CookieAuthenticationHandler).Assembly;

        builder.AddCratisArc(configureOptions: options =>
        {
            // Bind to both 127.0.0.1 and localhost so the WebView and any other
            // local clients can reach the backend regardless of hostname resolution.
            options.Hosting.ApplicationUrl = $"{LoopbackHost}:{Port};{LoopbackAlias}:{Port}";
            options.GeneratedApis.RoutePrefix = "api";
            options.GeneratedApis.IncludeCommandNameInRoute = false;
            options.GeneratedApis.SegmentsToSkipForRoute = 1;
        });

        builder.Services.AddSingleton<IAuthenticationHandler, CookieAuthenticationHandler>();
        builder.Services.AddSingleton<IAuthenticationHandler, MicrosoftIdentityPlatformHeaderAuthenticationHandler>();

        _app = builder.Build();

        // Serve the pre-built React frontend from wwwroot/ (same origin as the API).
        // On Mac Catalyst, Content files land in Contents/Resources/ inside the app bundle,
        // but AppContext.BaseDirectory points to Contents/MonoBundle/, so we must use
        // NSBundle.MainBundle.ResourcePath to get the correct bundle resources path.
        // On all other platforms AppContext.BaseDirectory is the right base.
        _app.UseStaticFiles(options =>
        {
#if MACCATALYST
            options.FileSystemPath = Path.Combine(Foundation.NSBundle.MainBundle.ResourcePath, "wwwroot");
#else
            options.FileSystemPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
#endif
        });
        _app.MapFallbackToFile();

        // UseCratisArc must be last so all endpoints are registered before Arc processes routes.
        _app.UseCratisArc();

        await _app.StartAsync();

        logger.Started(Port);
    }

    /// <summary>
    /// Stops and disposes the Arc backend.
    /// </summary>
    public async Task StopAsync()
    {
        if (_app is null)
        {
            return;
        }

        logger.Stopping();

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await _app.StopAsync(cts.Token);
        }
        catch (Exception ex)
        {
            logger.StopFailed(ex);
        }

        await _app.DisposeAsync();
        _app = null;

        logger.Stopped();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() => await StopAsync();
}
