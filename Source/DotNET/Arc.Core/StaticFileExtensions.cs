// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc;

/// <summary>
/// Extension methods for <see cref="ArcApplication"/> to serve static files.
/// </summary>
public static class StaticFileExtensions
{
    /// <summary>
    /// Configures the application to serve static files from the default "wwwroot" directory.
    /// </summary>
    /// <param name="app">The <see cref="ArcApplication"/>.</param>
    /// <returns>The <see cref="ArcApplication"/> for continuation.</returns>
    public static ArcApplication UseStaticFiles(this ArcApplication app)
    {
        return app.UseStaticFiles(new StaticFileOptions());
    }

    /// <summary>
    /// Configures the application to serve static files with the specified options.
    /// </summary>
    /// <param name="app">The <see cref="ArcApplication"/>.</param>
    /// <param name="options">The options for static file serving.</param>
    /// <returns>The <see cref="ArcApplication"/> for continuation.</returns>
    public static ArcApplication UseStaticFiles(this ArcApplication app, StaticFileOptions options)
    {
        if (app.EndpointMapper is HttpListenerEndpointMapper httpListenerEndpointMapper)
        {
            httpListenerEndpointMapper.UseStaticFiles(options);
        }

        return app;
    }

    /// <summary>
    /// Configures the application to serve static files with custom configuration.
    /// </summary>
    /// <param name="app">The <see cref="ArcApplication"/>.</param>
    /// <param name="configure">An action to configure the static file options.</param>
    /// <returns>The <see cref="ArcApplication"/> for continuation.</returns>
    public static ArcApplication UseStaticFiles(this ArcApplication app, Action<StaticFileOptions> configure)
    {
        var options = new StaticFileOptions();
        configure(options);
        return app.UseStaticFiles(options);
    }

    /// <summary>
    /// Configures a fallback file to serve when no route or static file matches the request.
    /// This is typically used for Single Page Applications (SPAs) to serve index.html for client-side routing.
    /// </summary>
    /// <param name="app">The <see cref="ArcApplication"/>.</param>
    /// <param name="filePath">The path to the fallback file, relative to the static files directory (default: "index.html").</param>
    /// <returns>The <see cref="ArcApplication"/> for continuation.</returns>
    public static ArcApplication MapFallbackToFile(this ArcApplication app, string filePath = "index.html")
    {
        if (app.EndpointMapper is HttpListenerEndpointMapper httpListenerEndpointMapper)
        {
            httpListenerEndpointMapper.MapFallbackToFile(filePath);
        }

        return app;
    }
}
