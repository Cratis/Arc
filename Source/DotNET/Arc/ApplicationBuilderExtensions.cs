// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for the application builder.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Use Cratis default setup.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to extend.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder UseCratisArc(this IApplicationBuilder app)
    {
        Internals.ServiceProvider = app.ApplicationServices;
        app.MapIdentityProvider();
        app.UseCommandEndpoints();
        app.UseQueryEndpoints();

        return app;
    }
}
