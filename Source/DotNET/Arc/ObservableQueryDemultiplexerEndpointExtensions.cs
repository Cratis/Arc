// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for mapping observable query demultiplexer endpoints.
/// </summary>
public static class ObservableQueryDemultiplexerEndpointExtensions
{
    /// <summary>
    /// Maps the observable query demultiplexer endpoints (<c>/.cratis/queries/ws</c> and <c>/.cratis/queries/sse</c>)
    /// for composite real-time query streaming.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to configure.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
    public static IApplicationBuilder UseObservableQueryDemultiplexer(this IApplicationBuilder app)
    {
        if (app is IEndpointRouteBuilder endpoints)
        {
            var mapper = new AspNetCoreEndpointMapper(endpoints);
            mapper.MapObservableQueryDemultiplexerEndpoints(app.ApplicationServices);
        }

        return app;
    }
}
