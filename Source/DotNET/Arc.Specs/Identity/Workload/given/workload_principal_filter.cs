// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Cratis.Arc.Identity.Workload.given;

[IgnoreConvention]
internal sealed class workload_principal_filter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, continuation) =>
        {
            var identity = new ClaimsIdentity([new("sub", "unsigned-person"), new(ClaimTypes.Role, "administrator"), new("email", "fixture@example.test")], "unrelated-product-scheme");
            if (context.Request.Headers.TryGetValue("x-fixture-tenant-claim", out var tenant)) identity.AddClaim(new("tenant_id", tenant.ToString()));
            context.User = new ClaimsPrincipal(identity);
            await continuation();
        });
        next(app);
    };
}
