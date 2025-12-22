// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for <see cref="IApplicationBuilder"/> for configuring Cratis.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Use Cratis middleware and endpoints.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to extend.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder UseCratis(this IApplicationBuilder app)
    {
        app.UseCratisArc();
        app.UseSwagger();
        app.UseSwaggerUI();
        return app;
    }
}
