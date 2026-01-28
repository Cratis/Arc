// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Http;

/// <summary>
/// Middleware for serving static files.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="StaticFilesMiddleware"/> class.
/// </remarks>
/// <param name="logger">The logger for the middleware.</param>
public class StaticFilesMiddleware(ILogger<StaticFilesMiddleware> logger) : IHttpRequestMiddleware
{
    readonly List<StaticFileOptions> _staticFileConfigurations = [];
    readonly ILogger<StaticFilesMiddleware> _logger = logger;

    /// <summary>
    /// Adds a static file configuration to the middleware.
    /// </summary>
    /// <param name="options">The options for static file serving.</param>
    public void AddConfiguration(StaticFileOptions options)
    {
        MimeTypes.AddMappings(options.ContentTypeMappings);
        _staticFileConfigurations.Add(options);
    }

    /// <inheritdoc/>
    public async Task<bool> InvokeAsync(HttpListenerContext context, HttpRequestDelegate next)
    {
        var method = context.Request.HttpMethod;
        var requestPath = context.Request.Url?.AbsolutePath ?? "/";

        // Only handle GET requests
        if (method != "GET")
        {
            return await next(context);
        }

        logger.LogDebug("Attempting to serve static file for {RequestPath}", requestPath);

        if (await TryServeStaticFileAsync(context, requestPath))
        {
            return true;
        }

        return await next(context);
    }

    async Task<bool> TryServeStaticFileAsync(HttpListenerContext context, string requestPath)
    {
        logger.LogDebug("Checking {ConfigCount} static file configurations", _staticFileConfigurations.Count);

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
            var directoryExists = Directory.Exists(filePath);
            if (directoryExists && config.ServeDefaultFiles)
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

            var fileExists = File.Exists(filePath);
            if (fileExists)
            {
                logger.LogDebug("Serving static file: {FilePath}", filePath);
                await ServeFileAsync(context, filePath);
                return true;
            }

            logger.LogDebug(
                "Static file not found: {FilePath} (directory exists: {DirectoryExists}, file exists: {FileExists})",
                filePath,
                directoryExists,
                fileExists);
        }

        return false;
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

        // Use current directory first (typical for development with dotnet run)
        // Fall back to AppContext.BaseDirectory if not found
        var currentDirPath = Path.Combine(Directory.GetCurrentDirectory(), fileSystemPath);
        if (Directory.Exists(currentDirPath))
        {
            return currentDirPath;
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
}
