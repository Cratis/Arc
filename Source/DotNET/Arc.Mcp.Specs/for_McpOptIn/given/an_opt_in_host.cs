// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Mcp.for_McpOptIn.given;

public class an_opt_in_host : Specification
{
    protected WebApplication _app;

    protected void Build(bool withMcp)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddCratisArc(configureBuilder: arc =>
        {
            arc.WithoutControllers();
            if (withMcp)
            {
                arc.WithMcp(mcp => mcp.Configure(options => options.ApplicationName = "app"));
            }
        });

        // This host tests routing opt-in, not artifacts discovered from optional adapter dependencies.
        builder.Services.AddSingleton<IInstancesOf<ICommandHandlerProvider>>(new KnownInstancesOf<ICommandHandlerProvider>());
        builder.Services.AddSingleton<IInstancesOf<IQueryPerformerProvider>>(new KnownInstancesOf<IQueryPerformerProvider>());
        _app = builder.Build();
        _app.UseCratisArc();
    }

    async Task Destroy() => await _app.DisposeAsync();
}
