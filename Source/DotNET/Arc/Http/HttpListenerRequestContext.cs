// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.Json;
using Cratis.Json;

namespace Cratis.Arc.Http;

/// <summary>
/// Implementation of <see cref="IHttpRequestContext"/> for <see cref="HttpListenerContext"/>.
/// </summary>
/// <param name="context">The <see cref="HttpListenerContext"/>.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
public class HttpListenerRequestContext(HttpListenerContext context, IServiceProvider serviceProvider) : IHttpRequestContext
{
    static readonly JsonSerializerOptions _jsonOptions = Globals.JsonSerializerOptions;
    readonly HttpListenerContext _context = context;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Query => ParseQueryString(_context.Request.QueryString);

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Headers => ParseHeaders(_context.Request.Headers);

    /// <inheritdoc/>
    public string Path => _context.Request.Url?.AbsolutePath ?? string.Empty;

    /// <inheritdoc/>
    public string Method => _context.Request.HttpMethod;

    /// <inheritdoc/>
    public IServiceProvider RequestServices { get; } = serviceProvider;

    /// <inheritdoc/>
    public CancellationToken RequestAborted => CancellationToken.None; // HttpListener doesn't provide cancellation token

    /// <inheritdoc/>
    public IWebSocketContext WebSockets { get; } = new HttpListenerWebSocketContext(context);

    /// <inheritdoc/>
    public async Task<object?> ReadBodyAsJsonAsync(Type type, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(_context.Request.InputStream, _context.Request.ContentEncoding);
        var json = await reader.ReadToEndAsync(cancellationToken);
        return JsonSerializer.Deserialize(json, type, _jsonOptions);
    }

    /// <inheritdoc/>
    public void SetStatusCode(int statusCode)
    {
        _context.Response.StatusCode = statusCode;
    }

    /// <inheritdoc/>
    public void SetResponseHeader(string name, string value)
    {
        _context.Response.Headers[name] = value;
    }

    /// <inheritdoc/>
    public async Task WriteResponseAsJsonAsync(object? value, Type type, CancellationToken cancellationToken = default)
    {
        _context.Response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(value, type, _jsonOptions);
        var buffer = Encoding.UTF8.GetBytes(json);
        _context.Response.ContentLength64 = buffer.Length;
        await _context.Response.OutputStream.WriteAsync(buffer, cancellationToken);
    }

    static ReadOnlyDictionary<string, string> ParseQueryString(NameValueCollection? queryString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (queryString is not null)
        {
            foreach (var key in queryString.AllKeys)
            {
                if (key is not null)
                {
                    result[key] = queryString[key] ?? string.Empty;
                }
            }
        }
        return result.AsReadOnly();
    }

    static ReadOnlyDictionary<string, string> ParseHeaders(NameValueCollection headers)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in headers.AllKeys)
        {
            if (key is not null)
            {
                result[key] = headers[key] ?? string.Empty;
            }
        }
        return result.AsReadOnly();
    }
}
