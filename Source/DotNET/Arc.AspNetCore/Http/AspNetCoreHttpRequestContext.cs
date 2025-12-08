// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    /// <summary>
    /// Gets the underlying <see cref="HttpContext"/>.
    /// </summary>
    /// <returns>The <see cref="HttpContext"/>.</returns>
    public HttpContext GetHttpContext() => httpContext;

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
}
