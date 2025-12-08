// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Execution;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Cratis.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for adding query endpoints.
/// </summary>
public static class QueryEndpointsExtensions
{
    /// <summary>
    /// Use Cratis query endpoints.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to extend.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder UseQueryEndpoints(this IApplicationBuilder app)
    {
        if (app is IEndpointRouteBuilder endpoints)
        {
            var arcOptions = app.ApplicationServices.GetRequiredService<IOptions<ArcOptions>>().Value;
            var options = arcOptions.GeneratedApis;
            var correlationIdAccessor = app.ApplicationServices.GetRequiredService<ICorrelationIdAccessor>();
            var queryPipeline = app.ApplicationServices.GetRequiredService<IQueryPipeline>();
            var queryPerformerProviders = app.ApplicationServices.GetRequiredService<IQueryPerformerProviders>();
            var jsonSerializerOptions = Globals.JsonSerializerOptions;

            var prefix = options.RoutePrefix.Trim('/');
            var group = endpoints.MapGroup($"/{prefix}");

            foreach (var performer in queryPerformerProviders.Performers)
            {
                var location = performer.Location.Skip(options.SegmentsToSkipForRoute);
                var segments = location.Select(segment => segment.ToKebabCase());
                var baseUrl = $"/{string.Join('/', segments)}";
                var typeName = options.IncludeQueryNameInRoute ? performer.Name.ToString() : string.Empty;

                var url = options.IncludeQueryNameInRoute ? $"{baseUrl}/{typeName.ToKebabCase()}" : baseUrl;
                url = url.ToLowerInvariant();

                var executeEndpointName = $"Execute{performer.Name}";
                if (!endpoints.EndpointExists(executeEndpointName))
                {
                    // Note: If we use the minimal API "MapPost" with HttpContext parameter, it does not show up in Swagger
                    //       So we use HttpRequest and HttpResponse instead
                    var builder = group.MapGet(url, async (HttpRequest request, HttpResponse response) =>
                    {
                        var context = request.HttpContext;
                        context.HandleCorrelationId(correlationIdAccessor, arcOptions.CorrelationId);

                        var paging = context.GetPagingInfo();
                        var sorting = context.GetSortingInfo();
                        var arguments = context.GetQueryArguments(performer);

                        // Perform the query first
                        var queryResult = await queryPipeline.Perform(performer.FullyQualifiedName, arguments, paging, sorting);

                        // Check if the result is a streaming result (Subject or AsyncEnumerable)
                        var webSocketQueryHandler = context.RequestServices.GetRequiredService<IObservableQueryHandler>();
                        if (queryResult.IsSuccess && webSocketQueryHandler.IsStreamingResult(queryResult.Data))
                        {
                            // Handle streaming results - both WebSocket and HTTP JSON streaming
                            var correlationId = correlationIdAccessor.Current != CorrelationId.NotSet ?
                                correlationIdAccessor.Current : CorrelationId.New();
                            var queryContext = new QueryContext(performer.FullyQualifiedName, correlationId, paging, sorting, arguments, []);
                            await webSocketQueryHandler.HandleStreamingResult(context, performer.Name, queryResult.Data!, queryContext);
                            return;
                        }

                        // Handle non-streaming results
                        response.SetResponseStatusCode(queryResult);
                        await response.WriteAsJsonAsync(queryResult, jsonSerializerOptions, cancellationToken: context.RequestAborted);
                    })
                    .WithTags(string.Join('.', location))
                    .WithName(executeEndpointName)
                    .WithSummary($"Execute {performer.Name} query");

                    if (performer.AllowsAnonymousAccess)
                    {
                        builder.AllowAnonymous();
                    }
                }
            }
        }

        return app;
    }
}