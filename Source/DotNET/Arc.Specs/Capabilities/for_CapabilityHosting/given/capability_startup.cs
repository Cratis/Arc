// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;

namespace Cratis.Arc.Capabilities.for_CapabilityHosting.given;

public class capability_startup
{
    public void Configure(IApplicationBuilder app, startup_capability_endpoints handlers)
    {
        app.UseRouting();
        app.UseCratisArc();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/mcp-spec", handlers.Selected);
            endpoints.MapGet("/legacy/ping", () => "legacy");
        });
    }
}
