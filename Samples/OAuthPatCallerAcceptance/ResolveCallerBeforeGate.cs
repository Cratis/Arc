// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Identity.Callers;

namespace OAuthPatCallerAcceptance;

/// <summary>
/// Exercises a caller cached before Arc's capability startup filter runs.
/// </summary>
public sealed class ResolveCallerBeforeGate : IStartupFilter
{
    /// <inheritdoc/>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.UseMiddleware<HttpRequestContextMiddleware>();
        app.Use(async (context, proceed) =>
        {
            _ = await context.RequestServices.GetRequiredService<IVerifiedCallerAccessor>().GetCurrent();
            await proceed(context);
        });
        next(app);
    };
}
