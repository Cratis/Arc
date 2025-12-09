// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.Json;

namespace Cratis.Arc.AspNetCore.Http;

/// <summary>
/// ASP.NET Core implementation of <see cref="IHttpRequestContext"/>.
/// </summary>
/// <param name="httpContext">The ASP.NET Core <see cref="HttpContext"/>.</param>
public class AspNetCoreHttpRequestContext(HttpContext httpContext) : IHttpRequestContext
{
    static readonly JsonSerializerOptions _jsonOptions = Globals.JsonSerializerOptions;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Query => httpContext.Request.Query.ToDictionary(
        kvp => kvp.Key,
        kvp => kvp.Value.ToString());

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Headers => httpContext.Request.Headers.ToDictionary(
        kvp => kvp.Key,
        kvp => kvp.Value.ToString());

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
    public ClaimsPrincipal User => httpContext.User;

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

    /// <inheritdoc/>
    public async Task<object?> ReadBodyAsJsonAsync(Type type, CancellationToken cancellationToken = default)
    {
        return await httpContext.Request.ReadFromJsonAsync(type, _jsonOptions, cancellationToken);
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
    public async Task WriteResponseAsJsonAsync(object? value, Type type, CancellationToken cancellationToken = default)
    {
        await httpContext.Response.WriteAsJsonAsync(value, type, _jsonOptions, cancellationToken);
    }

    /// <inheritdoc/>
    public void AppendCookie(string key, string value, Cratis.Arc.Http.CookieOptions options)
    {
        var aspNetCoreOptions = new Microsoft.AspNetCore.Http.CookieOptions
        {
            HttpOnly = options.HttpOnly,
            Secure = options.Secure,
            SameSite = options.SameSite switch
            {
                Cratis.Arc.Http.SameSiteMode.None => Microsoft.AspNetCore.Http.SameSiteMode.None,
                Cratis.Arc.Http.SameSiteMode.Lax => Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                Cratis.Arc.Http.SameSiteMode.Strict => Microsoft.AspNetCore.Http.SameSiteMode.Strict,
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
    public async Task WriteAsync(string text, CancellationToken cancellationToken = default)
    {
        await httpContext.Response.WriteAsync(text, cancellationToken);
    }
}
