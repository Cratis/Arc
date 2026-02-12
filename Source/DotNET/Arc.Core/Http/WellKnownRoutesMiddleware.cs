// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Http;

/// <summary>
/// Middleware for handling well-known routes (e.g., commands, queries).
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="WellKnownRoutesMiddleware"/> class.
/// </remarks>
/// <param name="serviceProvider">The service provider for request scoping.</param>
/// <param name="logger">The logger for the middleware.</param>
public class WellKnownRoutesMiddleware(IServiceProvider serviceProvider, ILogger<WellKnownRoutesMiddleware> logger) : IHttpRequestMiddleware
{
    readonly Dictionary<string, RouteHandler> _routes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a route with the middleware.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The handler for the request.</param>
    /// <param name="metadata">Optional metadata for the endpoint.</param>
    public void RegisterRoute(string method, string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null)
    {
        var routeKey = $"{method}:{pattern}";
        _routes[routeKey] = new RouteHandler(pattern, handler, metadata);
        logger.RegisteringRoute(method, pattern);
    }

    /// <inheritdoc/>
    public async Task<bool> InvokeAsync(HttpListenerContext context, HttpRequestDelegate next)
    {
        var method = context.Request.HttpMethod;
        var path = context.Request.Url?.AbsolutePath ?? "/";
        var routeKey = $"{method}:{path}";

        logger.CheckingRouteMatch(routeKey);

        if (_routes.TryGetValue(routeKey, out var route))
        {
            logger.RouteMatched(routeKey);

            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var requestContext = new HttpListenerRequestContext(context, scope.ServiceProvider);
                var httpRequestContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpRequestContextAccessor>();
                httpRequestContextAccessor.Current = requestContext;

                var authentication = scope.ServiceProvider.GetRequiredService<IAuthentication>();
                var authenticationMiddleware = new AuthenticationMiddleware(authentication);

                if (!await authenticationMiddleware.AuthenticateAsync(requestContext, route.Metadata))
                {
                    return true;
                }

                await route.Handler(requestContext);
                return true;
            }
            catch
            {
                // Let the exception propagate
                throw;
            }
        }

        return await next(context);
    }

    record RouteHandler(string Pattern, Func<IHttpRequestContext, Task> Handler, EndpointMetadata? Metadata);
}
