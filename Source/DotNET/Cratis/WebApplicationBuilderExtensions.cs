// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Swagger;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for <see cref="WebApplicationBuilder"/> for configuring Cratis.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Add Cratis to the <see cref="WebApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder"><see cref="WebApplicationBuilder"/> to extend.</param>
    /// <param name="configSectionPath">The optional configuration section path.</param>
    /// <returns><see cref="WebApplicationBuilder"/> for building continuation.</returns>
    public static WebApplicationBuilder AddCratis(this WebApplicationBuilder builder, string? configSectionPath = null)
    {
        builder.AddCratisArc(
            arcBuilder => arcBuilder.WithChronicle(),
            configSectionPath);

        builder.UseCratisMongoDB();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options => options.AddConcepts());

        return builder;
    }
}
