// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Execution;
using Microsoft.AspNetCore.Hosting;

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

            // Continue with the rest of the pipeline
            next(app);
        };
    }
}