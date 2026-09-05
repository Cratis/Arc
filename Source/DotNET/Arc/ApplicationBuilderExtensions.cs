// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for the application builder.
/// </summary>
public static class ApplicationBuilderExtensions
{
    const string CratisArcInitializedKey = "Cratis.Arc.Initialized";

    /// <summary>
    /// Activates Cratis Arc on the application, mapping the identity, introspection, command, and query
    /// endpoints. Call this after <c>AddCratisArc</c>. Calling it more than once on the same application is a
    /// no-op.
    /// </summary>
    /// <remarks>
    /// This activates Arc only; it does not start the Chronicle client. When you use event sourcing, also call
    /// <c>UseCratisChronicle</c> — or use <c>UseCratis</c>, which activates both halves.
    /// </remarks>
    /// <param name="app"><see cref="IApplicationBuilder"/> to extend.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder UseCratisArc(this IApplicationBuilder app)
    {
        // Prevent double initialization for the same app instance
        if (app.Properties.ContainsKey(CratisArcInitializedKey))
        {
            return app;
        }
        app.Properties[CratisArcInitializedKey] = true;

        Cratis.Arc.Internals.ServiceProvider = app.ApplicationServices;

        app.MapIdentityProvider();
        app.MapIntrospectionEndpoints();
        app.UseCommandEndpoints();
        app.UseQueryEndpoints();
        app.UseObservableQueryDemultiplexer();

        return app;
    }
}
