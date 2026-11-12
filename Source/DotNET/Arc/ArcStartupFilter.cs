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

            var hasCapabilities = app.ApplicationServices.HasCapabilities();
            var capabilityOptions = hasCapabilities ? app.ApplicationServices.GetRequiredService<IOptions<CapabilitiesOptions>>().Value : null;
            var selectiveCapabilities = capabilityOptions is { GatedEndpoints: not null } or { GateAfterRouting: true };

            // Selective hosting and gating after routing need routing before admission; the plain default gate stays pre-routing.
            IEndpointRouteBuilder? capabilityRoutes = null;
            if (selectiveCapabilities)
            {
                app.UseRouting();
                capabilityRoutes = app.Properties.TryGetValue("__EndpointRouteBuilder", out var earlyRoutes)
                    ? earlyRoutes as IEndpointRouteBuilder
                    : null;
            }
            if (hasCapabilities)
            {
                app.UseMiddleware<CapabilityGateMiddleware>();
            }

            // Continue with the rest of the pipeline before compiling the complete endpoint graph.
            next(app);
            if (selectiveCapabilities)
            {
                CompileSelection(app, capabilityRoutes);
            }
        };
    }

    static void CompileSelection(IApplicationBuilder app, IEndpointRouteBuilder? capabilityRoutes)
    {
        var routes = app.Properties.TryGetValue("__EndpointRouteBuilder", out var routeBuilder)
            ? routeBuilder as IEndpointRouteBuilder
            : null;

        // The host may create its own route builder. Populate the gate's early matcher with the
        // completed graph's exact endpoint instances, or refuse startup if no rules can be matched.
        if (capabilityRoutes?.DataSources.Count == 0)
        {
            var endpoints = routes?.DataSources.SelectMany(source => source.Endpoints).ToArray() ?? [];
            if (endpoints.Length == 0 && app.ApplicationServices.GetService<EndpointDataSource>() is { } source)
            {
                endpoints = source.Endpoints.ToArray();
            }
            capabilityRoutes.DataSources.Add(new DefaultEndpointDataSource(endpoints));
        }
        app.ApplicationServices.GetRequiredService<CapabilityEndpointSelection>().Compile(capabilityRoutes?.DataSources ?? []);
    }
}