// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Screenplay.Embedded.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for exposing the embedded event model explorer from a Cratis application.
/// </summary>
public static class EventModelViewerApplicationBuilderExtensions
{
    /// <summary>
    /// Exposes the embedded event model explorer when the application was built for development, serving the
    /// documents embedded in the application and the assemblies it references.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to expose the explorer from.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for continuation.</returns>
    /// <exception cref="Cratis.Arc.Screenplay.Embedded.Hosting.Catalog.MalformedEventModelCatalog">Thrown when an assembly embeds a catalog that cannot be read as written.</exception>
    /// <remarks>
    /// <para>
    /// This is what <c>UseCratis</c> calls, so an application using the Cratis meta-package gets the explorer in
    /// a Debug build without asking for it, and a Release build exposes nothing - the entry assembly's
    /// <see cref="System.Diagnostics.DebuggableAttribute"/> decides, not an environment name a deployment can get
    /// wrong. <c>EventModelViewerOptions.Enabled</c> overrides the decision either way, and
    /// <c>EventModelViewerOptions.RequireAuthorization</c> puts the explorer behind the host's authorization.
    /// </para>
    /// <para>
    /// Mapping the explorer yourself with <c>MapCratisEventModel</c> keeps working and stays authoritative about
    /// the assemblies it names: the routes are mapped once, and whichever call comes second adds to what the
    /// first one serves.
    /// </para>
    /// </remarks>
    public static IApplicationBuilder UseCratisEventModelViewer(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app is not IEndpointRouteBuilder endpoints)
        {
            return app;
        }

        var options = app.ApplicationServices.GetService<IOptions<EventModelViewerOptions>>()?.Value ?? new EventModelViewerOptions();
        var entryAssembly = Assembly.GetEntryAssembly();
        if (!EventModelViewerExposure.ShouldExpose(options, entryAssembly))
        {
            return app;
        }

        var assemblies = EventModelViewerAssemblies.For(entryAssembly, AppDomain.CurrentDomain.GetAssemblies(), options.Assemblies);
        if (assemblies.Count == 0)
        {
            return app;
        }

        var group = EventModelEndpoints.Map(endpoints, assemblies);
        if (options.RequireAuthorization)
        {
            if (options.AuthorizationPolicy is { Length: > 0 } policy)
            {
                group.RequireAuthorization(policy);
            }
            else
            {
                group.RequireAuthorization();
            }
        }

        return app;
    }
}
