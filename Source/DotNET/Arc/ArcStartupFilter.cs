// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Capabilities;
using Cratis.Arc.Execution;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Arc;

/// <summary>
/// Represents an implementation of <see cref="IStartupFilter"/> that configures Arc middleware at the beginning of the pipeline.
/// </summary>
public class ArcStartupFilter : IStartupFilter
{
    /// <inheritdoc/>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            // Add Arc middlewares at the beginning of the pipeline. The request context is published first so it is
            // available to everything downstream — tenant resolution in particular, which otherwise silently sees no
            // request and resolves no tenant when it runs before an endpoint delegate assigns the context.
            app.UseMiddleware<HttpRequestContextMiddleware>();
            app.UseMiddleware<CorrelationIdMiddleware>();

            var hasCapabilities = app.ApplicationServices.GetService<CapabilityGateMiddleware>() is not null;
            var selectiveCapabilities = hasCapabilities && app.ApplicationServices.GetRequiredService<IOptions<CapabilitiesOptions>>().Value.GatedEndpoints is not null;

            // Only selective hosting needs routing before admission; the default gate stays pre-routing.
            if (selectiveCapabilities)
            {
                app.UseRouting();
            }
            if (hasCapabilities)
            {
                app.UseMiddleware<CapabilityGateMiddleware>();
            }

            // Continue with the rest of the pipeline before compiling the complete endpoint graph.
            next(app);
            if (selectiveCapabilities)
            {
                CompileSelection(app);
            }
        };
    }

    static void CompileSelection(IApplicationBuilder app)
    {
        var routes = app.Properties.TryGetValue("__EndpointRouteBuilder", out var routeBuilder)
            ? routeBuilder as IEndpointRouteBuilder
            : null;

        // Minimal hosting restores this filter's route builder after configuring its own graph.
        // Give the early matcher the same startup endpoint instances, not a separate path matcher.
        if (routes?.DataSources.Count == 0 && app.ApplicationServices.GetService<EndpointDataSource>() is { } source)
        {
            routes.DataSources.Add(new DefaultEndpointDataSource(source.Endpoints));
        }
        app.ApplicationServices.GetRequiredService<CapabilityEndpointSelection>().Compile(routes?.DataSources ?? []);
    }
}