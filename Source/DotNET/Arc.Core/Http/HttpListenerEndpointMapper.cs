// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Http;

/// <summary>
/// Implementation of <see cref="IEndpointMapper"/> using <see cref="HttpListener"/>.
/// </summary>
public class HttpListenerEndpointMapper : IEndpointMapper, IDisposable
{
    readonly HttpListener _listener = new();
    readonly Dictionary<string, RouteHandler> _routes = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, EndpointMetadata> _endpoints = [];
    readonly ILogger<HttpListenerEndpointMapper> _logger;
    IServiceProvider? _serviceProvider;
    Task? _listenerTask;
    CancellationTokenSource? _cancellationTokenSource;
    string _pathBase = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpListenerEndpointMapper"/> class.
    /// </summary>
    /// <param name="logger">The logger for the endpoint mapper.</param>
    /// <param name="prefixes">Optional HTTP prefixes to listen on. Defaults to http://localhost:5000/.</param>
    public HttpListenerEndpointMapper(ILogger<HttpListenerEndpointMapper> logger, params string[] prefixes)
    {
        _logger = logger;
        var prefixList = prefixes.Length > 0 ? prefixes : ["http://localhost:5000/"];

        _logger.AddingHttpListenerPrefixes(prefixList.Length);
        foreach (var prefix in prefixList)
        {
            _logger.AddingHttpListenerPrefix(prefix);
            _listener.Prefixes.Add(prefix);
        }
    }

    /// <inheritdoc/>
    public void MapGet(string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null)
    {
        MapRoute("GET", pattern, handler, metadata);
    }

    /// <inheritdoc/>
    public void MapPost(string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null)
    {
        MapRoute("POST", pattern, handler, metadata);
    }

    /// <inheritdoc/>
    public bool EndpointExists(string name)
    {
        return _endpoints.ContainsKey(name);
    }

    /// <summary>
    /// Starts the HTTP listener.
    /// </summary>
    /// <param name="serviceProvider">The service provider for request scoping.</param>
    public void Start(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _cancellationTokenSource = new CancellationTokenSource();
        _listener.Start();
        _listenerTask = ListenAsync(_cancellationTokenSource.Token);

        _logger.HttpListenerStarted(string.Join(", ", _listener.Prefixes));
    }

    /// <summary>
    /// Stops the HTTP listener.
    /// </summary>
    /// <returns>A task representing the stop operation.</returns>
    public async Task StopAsync()
    {
        await _cancellationTokenSource?.CancelAsync();
        _listener.Stop();
        if (_listenerTask is not null)
        {
            await _listenerTask;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _listener.Close();
        _cancellationTokenSource?.Dispose();
    }

    /// <summary>
    /// Sets the path base for all endpoints.
    /// </summary>
    /// <param name="pathBase">The base path.</param>
    public void SetPathBase(string pathBase)
    {
        _pathBase = pathBase.TrimEnd('/');
    }

    void MapRoute(string method, string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata)
    {
        var fullPattern = string.IsNullOrEmpty(_pathBase) ? pattern : $"{_pathBase}{pattern}";
        var routeKey = $"{method}:{fullPattern}";
        _routes[routeKey] = new RouteHandler(fullPattern, handler, metadata);

        if (metadata is not null)
        {
            _endpoints[metadata.Name] = metadata;
        }
    }

    async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(async () => await HandleRequestAsync(context), cancellationToken);
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.ErrorInListenerLoop(ex);
            }
        }
    }

    async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            var method = context.Request.HttpMethod;
            var path = context.Request.Url?.AbsolutePath ?? "/";
            var routeKey = $"{method}:{path}";

            if (_routes.TryGetValue(routeKey, out var route))
            {
                await using var scope = _serviceProvider!.CreateAsyncScope();
                var requestContext = new HttpListenerRequestContext(context, scope.ServiceProvider);
                var httpRequestContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpRequestContextAccessor>();
                httpRequestContextAccessor.Current = requestContext;

                var authentication = scope.ServiceProvider.GetRequiredService<IAuthentication>();
                var authenticationMiddleware = new AuthenticationMiddleware(authentication);

                if (!await authenticationMiddleware.AuthenticateAsync(requestContext, route.Metadata))
                {
                    return;
                }

                await route.Handler(requestContext);
            }
            else
            {
                context.Response.StatusCode = 404;
                var buffer = System.Text.Encoding.UTF8.GetBytes("Not Found");
                await context.Response.OutputStream.WriteAsync(buffer);
            }
        }
        catch (Exception ex)
        {
            _logger.ErrorHandlingRequest(ex);
            context.Response.StatusCode = 500;
        }
        finally
        {
            context.Response.Close();
        }
    }

    record RouteHandler(string Pattern, Func<IHttpRequestContext, Task> Handler, EndpointMetadata? Metadata);
}
