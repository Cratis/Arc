// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Commands;
using Cratis.Execution;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for adding command endpoints.
/// </summary>
public static class CommandEndpointsExtensions
{
    /// <summary>
    /// Use Cratis command endpoints.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to extend.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder UseCommandEndpoints(this IApplicationBuilder app)
    {
        if (app is IEndpointRouteBuilder endpoints)
        {
            var mapper = new AspNetCoreEndpointMapper(endpoints);
            mapper.MapCommandEndpoints(app.ApplicationServices);

            // Map controller-based command validation endpoints
            var actionDescriptorProvider = app.ApplicationServices.GetRequiredService<IActionDescriptorCollectionProvider>();
            var commandPipeline = app.ApplicationServices.GetRequiredService<ICommandPipeline>();
            var correlationIdAccessor = app.ApplicationServices.GetRequiredService<ICorrelationIdAccessor>();
            var arcOptions = app.ApplicationServices.GetRequiredService<IOptions<ArcOptions>>().Value;

            var controllerCommandMapper = new ControllerCommandEndpointMapper(
                actionDescriptorProvider,
                commandPipeline,
                correlationIdAccessor,
                arcOptions.JsonSerializerOptions,
                mapper);
            controllerCommandMapper.MapValidationEndpoints(endpoints, arcOptions);
        }

        return app;
    }
}
