// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Http;

/// <summary>
/// Implementation of <see cref="IEndpointMapper"/> using <see cref="HttpListener"/>.
/// </summary>
public class HttpListenerEndpointMapper : IEndpointMapper, IDisposable
{
    readonly HttpListener _listener = new();
    readonly Dictionary<string, EndpointMetadata> _endpoints = [];
    readonly ILogger<HttpListenerEndpointMapper> _logger;
    readonly List<PendingRoute> _pendingRoutes = [];
    readonly List<RouteInfo> _registeredRoutes = [];
    readonly List<StaticFileOptions> _pendingStaticFileConfigurations = [];
    WellKnownRoutesMiddleware? _wellKnownRoutesMiddleware;
    StaticFilesMiddleware? _staticFilesMiddleware;
    FallbackMiddleware? _fallbackMiddleware;
    HttpRequestPipeline? _pipeline;
    Task? _listenerTask;
    CancellationTokenSource? _cancellationTokenSource;
    string _pathBase = string.Empty;
    string? _fallbackFilePath;
    bool _isStarted;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpListenerEndpointMapper"/> class.
    /// </summary>
    /// <param name="logger">The logger for the endpoint mapper.</param>
    /// <param name="prefixes">Optional HTTP prefixes to listen on. Defaults to http://localhost:5001/.</param>
    public HttpListenerEndpointMapper(ILogger<HttpListenerEndpointMapper> logger, params string[] prefixes)
    {
        _logger = logger;
        var prefixList = prefixes.Length > 0 ? prefixes : ["http://localhost:5001/"];

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
        if (_isStarted)
        {
            return;
        }

        // Initialize middlewares
        _wellKnownRoutesMiddleware = new WellKnownRoutesMiddleware(
            serviceProvider,
            serviceProvider.GetRequiredService<ILogger<WellKnownRoutesMiddleware>>());

        _staticFilesMiddleware = new StaticFilesMiddleware(
            serviceProvider.GetRequiredService<ILogger<StaticFilesMiddleware>>());

        _fallbackMiddleware = new FallbackMiddleware(
            serviceProvider.GetRequiredService<ILogger<FallbackMiddleware>>());

        // Register pending routes
        foreach (var route in _pendingRoutes)
        {
            _wellKnownRoutesMiddleware.RegisterRoute(route.Method, route.Pattern, route.Handler, route.Metadata);
        }
        _pendingRoutes.Clear();

        // Configure pending static file configurations
        foreach (var config in _pendingStaticFileConfigurations)
        {
            _staticFilesMiddleware.AddConfiguration(config);
        }

        // Configure fallback if set
        if (_fallbackFilePath is not null && _pendingStaticFileConfigurations.Count > 0)
        {
            var firstConfig = _pendingStaticFileConfigurations[0];
            _fallbackMiddleware.ConfigureFallback(_fallbackFilePath, firstConfig.FileSystemPath);
        }

        _pendingStaticFileConfigurations.Clear();

        // Build the pipeline
        var middlewares = new List<IHttpRequestMiddleware>
        {
            _wellKnownRoutesMiddleware,
            _staticFilesMiddleware,
            _fallbackMiddleware
        };

        _pipeline = new HttpRequestPipeline(
            middlewares,
            serviceProvider.GetRequiredService<ILogger<HttpRequestPipeline>>());

        _cancellationTokenSource = new CancellationTokenSource();
        _listener.Start();
        _listenerTask = Listen(_cancellationTokenSource.Token);
        _isStarted = true;

        _logger.HttpListenerStarted(string.Join(", ", _listener.Prefixes));
    }

    /// <summary>
    /// Stops the HTTP listener.
    /// </summary>
    /// <returns>A task representing the stop operation.</returns>
    public async Task Stop()
    {
        if (!_isStarted)
        {
            return;
        }

        await _cancellationTokenSource?.CancelAsync();
        _listener.Stop();
        if (_listenerTask is not null)
        {
            await _listenerTask;
        }

        _isStarted = false;
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

    /// <summary>
    /// Gets all registered routes with their metadata.
    /// </summary>
    /// <returns>A collection of route information.</returns>
    public IEnumerable<RouteInfo> GetRoutes()
    {
        return _registeredRoutes;
    }

    /// <summary>
    /// Configures static file serving with the specified options.
    /// </summary>
    /// <param name="options">The options for static file serving.</param>
    public void UseStaticFiles(StaticFileOptions options)
    {
        MimeTypes.AddMappings(options.ContentTypeMappings);

        if (_staticFilesMiddleware is not null)
        {
            _staticFilesMiddleware.AddConfiguration(options);
        }
        else
        {
            _pendingStaticFileConfigurations.Add(options);
        }
    }

    /// <summary>
    /// Sets a fallback file to serve when no route or static file matches the request.
    /// This is typically used for Single Page Applications (SPAs) to serve index.html for client-side routing.
    /// </summary>
    /// <param name="filePath">The path to the fallback file, relative to the first static file configuration's FileSystemPath.</param>
    public void MapFallbackToFile(string filePath)
    {
        _fallbackFilePath = filePath;
    }

    void MapRoute(string method, string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata)
    {
        var fullPattern = string.IsNullOrEmpty(_pathBase) ? pattern : $"{_pathBase}{pattern}";
        _logger.RegisteringRoute(method, fullPattern);

        // Track the route
        _registeredRoutes.Add(new RouteInfo(method, fullPattern, metadata));

        if (_wellKnownRoutesMiddleware is not null)
        {
            _wellKnownRoutesMiddleware.RegisterRoute(method, fullPattern, handler, metadata);
        }
        else
        {
            _pendingRoutes.Add(new PendingRoute(method, fullPattern, handler, metadata));
        }

        if (metadata is not null)
        {
            _endpoints[metadata.Name] = metadata;
        }
    }

    async Task Listen(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(async () => await HandleRequest(context), cancellationToken);
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.ErrorInListenerLoop(ex);
            }
        }
    }

    async Task HandleRequest(HttpListenerContext context)
    {
        try
        {
            var method = context.Request.HttpMethod;
            var path = context.Request.Url?.AbsolutePath ?? "/";

            _logger.IncomingRequest(method, path, context.Request.IsWebSocketRequest);

            await _pipeline!.ProcessAsync(context);
        }
        catch (HttpListenerException ex) when (ex.ErrorCode == 32 || ex.Message.Contains("Broken pipe"))
        {
            // Client disconnected - this is expected behavior, log at debug level
            _logger.ClientDisconnected(context.Request.Url?.AbsolutePath ?? "/");
        }
        catch (Exception ex)
        {
            _logger.ErrorHandlingRequest(ex);
            try
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
            catch
            {
                // Ignore errors when trying to send error response
            }
        }
    }

    record PendingRoute(string Method, string Pattern, Func<IHttpRequestContext, Task> Handler, EndpointMetadata? Metadata);
}

