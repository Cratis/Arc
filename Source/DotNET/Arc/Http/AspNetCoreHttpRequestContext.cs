// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.AspNetCore.Http;

/// <summary>
/// ASP.NET Core implementation of <see cref="IHttpRequestContext"/>.
/// </summary>
/// <param name="httpContext">The ASP.NET Core <see cref="HttpContext"/>.</param>
public class AspNetCoreHttpRequestContext(HttpContext httpContext) : IHttpRequestContext
{
    JsonSerializerOptions? _jsonOptions;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Query => httpContext.Request.Query.ToDictionary(
        kvp => kvp.Key,
        kvp => kvp.Value.ToString());

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Headers => httpContext.Request.Headers.ToDictionary(
        kvp => kvp.Key,
        kvp => kvp.Value.ToString());

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Cookies => httpContext.Request.Cookies.ToDictionary(
        kvp => kvp.Key,
        kvp => kvp.Value);

    /// <inheritdoc/>
    public string Path => httpContext.Request.Path;

    /// <inheritdoc/>
    public string Method => httpContext.Request.Method;

    /// <inheritdoc/>
    public IServiceProvider RequestServices => httpContext.RequestServices;

    /// <inheritdoc/>
    public CancellationToken RequestAborted => httpContext.RequestAborted;

    /// <inheritdoc/>
    public IWebSocketContext WebSockets { get; } = new AspNetCoreWebSocketContext(httpContext);

    /// <inheritdoc/>
    public ClaimsPrincipal User
    {
        get => httpContext.User;
        set => httpContext.User = value;
    }

    /// <inheritdoc/>
    public IDictionary<object, object?> Items => httpContext.Items;

    /// <inheritdoc/>
    public bool IsHttps => httpContext.Request.IsHttps;

    /// <inheritdoc/>
    public string? ContentType
    {
        get => httpContext.Response.ContentType;
        set => httpContext.Response.ContentType = value;
    }

    /// <inheritdoc/>
    public int StatusCode
    {
        get => httpContext.Response.StatusCode;
        set => httpContext.Response.StatusCode = value;
    }

    JsonSerializerOptions JsonSerializerOptions => _jsonOptions ??= httpContext.RequestServices.GetRequiredService<IOptions<ArcOptions>>().Value.JsonSerializerOptions;

    /// <inheritdoc/>
    public async Task<object?> ReadBodyAsJson(Type type, CancellationToken cancellationToken = default)
    {
        return await httpContext.Request.ReadFromJsonAsync(type, JsonSerializerOptions, cancellationToken);
    }

    /// <inheritdoc/>
    public void SetStatusCode(int statusCode)
    {
        httpContext.Response.StatusCode = statusCode;
    }

    /// <inheritdoc/>
    public void SetResponseHeader(string name, string value)
    {
        httpContext.Response.Headers[name] = value;
    }

    /// <inheritdoc/>
    public async Task WriteResponseAsJson(object? value, Type type, CancellationToken cancellationToken = default)
    {
        await httpContext.Response.WriteAsJsonAsync(value, type, JsonSerializerOptions, cancellationToken);
    }

    /// <inheritdoc/>
    public void AppendCookie(string key, string value, Arc.Http.CookieOptions options)
    {
        var aspNetCoreOptions = new Microsoft.AspNetCore.Http.CookieOptions
        {
            HttpOnly = options.HttpOnly,
            Secure = options.Secure,
            SameSite = options.SameSite switch
            {
                Arc.Http.SameSiteMode.None => Microsoft.AspNetCore.Http.SameSiteMode.None,
                Arc.Http.SameSiteMode.Lax => Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                Arc.Http.SameSiteMode.Strict => Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                _ => Microsoft.AspNetCore.Http.SameSiteMode.Unspecified
            },
            Path = options.Path,
            Expires = options.Expires,
            MaxAge = options.MaxAge,
            Domain = options.Domain
        };
        httpContext.Response.Cookies.Append(key, value, aspNetCoreOptions);
    }

    /// <inheritdoc/>
    public void RemoveCookie(string key)
    {
        httpContext.Response.Cookies.Delete(key);
    }

    /// <inheritdoc/>
    public async Task Write(string text, CancellationToken cancellationToken = default)
    {
        await httpContext.Response.WriteAsync(text, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task WriteBytes(byte[] data, CancellationToken cancellationToken = default)
    {
        await httpContext.Response.Body.WriteAsync(data, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task WriteStream(Stream stream, CancellationToken cancellationToken = default)
    {
        await stream.CopyToAsync(httpContext.Response.Body, cancellationToken);
    }
}
