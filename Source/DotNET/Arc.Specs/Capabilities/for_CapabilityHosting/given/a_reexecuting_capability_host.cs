// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Capabilities.for_CapabilityHosting.given;

public class a_reexecuting_capability_host : a_selective_capability_host
{
    protected virtual string ReexecutionPath => "/mcp-spec";

    protected override void ConfigurePipeline(WebApplication app)
    {
        app.UseStatusCodePagesWithReExecute(ReexecutionPath);
        app.UseExceptionHandler(ReexecutionPath);
        app.Use(async (context, next) =>
        {
            if (context.Request.Path == "/legacy/rewrite")
            {
                context.SetEndpoint(null);
                context.Request.Path = ReexecutionPath;
            }
            if (context.Request.Path == "/legacy/unknown")
            {
                context.SetEndpoint(new Endpoint(
                    _ =>
                    {
                        Interlocked.Increment(ref _selectedCalls);
                        return Task.CompletedTask;
                    },
                    EndpointMetadataCollection.Empty,
                    "runtime endpoint"));
            }
            await next(context);
        });
        app.UseRouting();
    }

    protected override void ConfigureEndpoints(WebApplication app)
    {
        app.MapGet("/legacy/status", context =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        });
        app.MapGet("/legacy/exception", context => Task.FromException(new InvalidOperationException("Re-execute the request.")));
    }
}
