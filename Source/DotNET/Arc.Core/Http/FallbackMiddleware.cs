// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Http;

/// <summary>
/// Middleware for serving a fallback file when no route matches (typically for SPAs).
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FallbackMiddleware"/> class.
/// </remarks>
/// <param name="logger">The logger for the middleware.</param>
public class FallbackMiddleware(ILogger<FallbackMiddleware> logger) : IHttpRequestMiddleware
{
    readonly ILogger<FallbackMiddleware> _logger = logger;
    string? _fallbackFilePath;
    string? _fileSystemBasePath;

    /// <summary>
    /// Configures the fallback file to serve.
    /// </summary>
    /// <param name="fallbackFilePath">The path to the fallback file, relative to the file system base path.</param>
    /// <param name="fileSystemBasePath">The base file system path.</param>
    public void ConfigureFallback(string fallbackFilePath, string fileSystemBasePath)
    {
        _fallbackFilePath = fallbackFilePath;
        _fileSystemBasePath = fileSystemBasePath;
    }

    /// <inheritdoc/>
    public async Task<bool> InvokeAsync(HttpListenerContext context, HttpRequestDelegate next)
    {
        var method = context.Request.HttpMethod;

        // Only handle GET requests
        if (method != "GET")
        {
            return await next(context);
        }

        if (await TryServeFallbackFileAsync(context))
        {
            context.Response.Close();
            return true;
        }

        return await next(context);
    }

    async Task<bool> TryServeFallbackFileAsync(HttpListenerContext context)
    {
        if (_fallbackFilePath is null || _fileSystemBasePath is null)
        {
            return false;
        }

        var fileSystemPath = GetAbsoluteFileSystemPath(_fileSystemBasePath);
        var filePath = Path.Combine(fileSystemPath, _fallbackFilePath.TrimStart('/'));

        // Ensure the resolved path is still within the configured file system path (prevent directory traversal)
        var fullResolvedPath = Path.GetFullPath(filePath);
        var fullBasePath = Path.GetFullPath(fileSystemPath);
        if (!fullResolvedPath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (File.Exists(filePath))
        {
            logger.LogDebug("Serving fallback file: {FilePath}", filePath);
            await ServeFileAsync(context, filePath);
            return true;
        }

        return false;
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
