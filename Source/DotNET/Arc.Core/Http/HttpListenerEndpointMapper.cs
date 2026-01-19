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
    readonly List<StaticFileOptions> _staticFileConfigurations = [];
    IServiceProvider? _serviceProvider;
    Task? _listenerTask;
    CancellationTokenSource? _cancellationTokenSource;
    string _pathBase = string.Empty;
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

        _serviceProvider = serviceProvider;
        _cancellationTokenSource = new CancellationTokenSource();
        _listener.Start();
        _listenerTask = ListenAsync(_cancellationTokenSource.Token);
        _isStarted = true;

        _logger.HttpListenerStarted(string.Join(", ", _listener.Prefixes));
    }

    /// <summary>
    /// Stops the HTTP listener.
    /// </summary>
    /// <returns>A task representing the stop operation.</returns>
    public async Task StopAsync()
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
        foreach (var kvp in _routes)
        {
            var parts = kvp.Key.Split(':', 2);
            var method = parts[0];
            var pattern = parts[1];

            yield return new RouteInfo(method, pattern, kvp.Value.Metadata);
        }
    }

    /// <summary>
    /// Configures static file serving with the specified options.
    /// </summary>
    /// <param name="options">The options for static file serving.</param>
    public void UseStaticFiles(StaticFileOptions options)
    {
        MimeTypes.AddMappings(options.ContentTypeMappings);
        _staticFileConfigurations.Add(options);
    }

    static string? GetRelativePath(string requestPath, string configuredRequestPath)
    {
        if (string.IsNullOrEmpty(configuredRequestPath) || configuredRequestPath == "/")
        {
            return requestPath;
        }

        var normalizedConfigPath = configuredRequestPath.TrimEnd('/');
        if (requestPath.Equals(normalizedConfigPath, StringComparison.OrdinalIgnoreCase) ||
            requestPath.StartsWith(normalizedConfigPath + "/", StringComparison.OrdinalIgnoreCase))
        {
            return requestPath[normalizedConfigPath.Length..];
        }

        return null;
    }

    static string GetAbsoluteFileSystemPath(string fileSystemPath)
    {
        if (Path.IsPathRooted(fileSystemPath))
        {
            return fileSystemPath;
        }

        return Path.Combine(AppContext.BaseDirectory, fileSystemPath);
    }

    static async Task ServeFileAsync(HttpListenerContext context, string filePath)
    {
        context.Response.ContentType = MimeTypes.GetMimeTypeFromPath(filePath);
        context.Response.ContentLength64 = new FileInfo(filePath).Length;

        await using var fileStream = File.OpenRead(filePath);
        await fileStream.CopyToAsync(context.Response.OutputStream);
    }

    void MapRoute(string method, string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata)
    {
        var fullPattern = string.IsNullOrEmpty(_pathBase) ? pattern : $"{_pathBase}{pattern}";
        var routeKey = $"{method}:{fullPattern}";
        _routes[routeKey] = new RouteHandler(fullPattern, handler, metadata);
        _logger.RegisteringRoute(method, fullPattern);

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

    async Task HandleRequestAsync(HttpListenerContext context)
    {
        var isWebSocketRequest = false;
        try
        {
            var method = context.Request.HttpMethod;
            var path = context.Request.Url?.AbsolutePath ?? "/";
            var routeKey = $"{method}:{path}";

            _logger.IncomingRequest(method, path, context.Request.IsWebSocketRequest);

            if (_routes.TryGetValue(routeKey, out var route))
            {
                _logger.RouteMatched(routeKey);

                // Check if this is a WebSocket request before processing
                isWebSocketRequest = context.Request.IsWebSocketRequest;

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
                // Try to serve static files if configured
                if (method == "GET" && await TryServeStaticFileAsync(context, path))
                {
                    return;
                }

                _logger.RouteNotFound(routeKey, string.Join(", ", _routes.Keys));
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
            // Don't close the response for WebSocket requests - the WebSocket connection
            // has already taken over the underlying connection and will handle its own lifecycle
            if (!isWebSocketRequest)
            {
                context.Response.Close();
            }
        }
    }

    async Task<bool> TryServeStaticFileAsync(HttpListenerContext context, string requestPath)
    {
        foreach (var config in _staticFileConfigurations)
        {
            var relativePath = GetRelativePath(requestPath, config.RequestPath);
            if (relativePath is null)
            {
                continue;
            }

            var fileSystemPath = GetAbsoluteFileSystemPath(config.FileSystemPath);
            var filePath = Path.Combine(fileSystemPath, relativePath.TrimStart('/'));

            // Handle directory requests with default files
            if (Directory.Exists(filePath) && config.ServeDefaultFiles)
            {
                foreach (var defaultFileName in config.DefaultFileNames)
                {
                    var defaultFilePath = Path.Combine(filePath, defaultFileName);
                    if (File.Exists(defaultFilePath))
                    {
                        filePath = defaultFilePath;
                        break;
                    }
                }
            }

            // Ensure the resolved path is still within the configured file system path (prevent directory traversal)
            var fullResolvedPath = Path.GetFullPath(filePath);
            var fullBasePath = Path.GetFullPath(fileSystemPath);
            if (!fullResolvedPath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (File.Exists(filePath))
            {
                await ServeFileAsync(context, filePath);
                return true;
            }
        }

        return false;
    }

    record RouteHandler(string Pattern, Func<IHttpRequestContext, Task> Handler, EndpointMetadata? Metadata);
}
