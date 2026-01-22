// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Http;

/// <summary>
/// Implementation of <see cref="IHttpRequestContext"/> for <see cref="HttpListenerContext"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="HttpListenerRequestContext"/> class.
/// </remarks>
/// <param name="context">The <see cref="HttpListenerContext"/>.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
public class HttpListenerRequestContext(HttpListenerContext context, IServiceProvider serviceProvider) : IHttpRequestContext
{
    readonly Dictionary<object, object?> _items = [];
    JsonSerializerOptions? _jsonOptions;
    IWebSocketContext? _webSockets;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Query { get; } = ParseQueryString(context.Request.QueryString);

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Headers { get; } = ParseHeaders(context.Request.Headers);

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Cookies { get; } = ParseCookies(context.Request.Cookies);

    /// <inheritdoc/>
    public string Path => context.Request.Url?.AbsolutePath ?? string.Empty;

    /// <inheritdoc/>
    public string Method => context.Request.HttpMethod;

    /// <inheritdoc/>
    public IServiceProvider RequestServices { get; } = serviceProvider;

    /// <inheritdoc/>
    public CancellationToken RequestAborted => CancellationToken.None; // HttpListener doesn't provide cancellation token

    /// <inheritdoc/>
    public IWebSocketContext WebSockets => _webSockets ??= new HttpListenerWebSocketContext(context);

    /// <inheritdoc/>
    public ClaimsPrincipal User { get; set; } = (context.User as ClaimsPrincipal) ?? new ClaimsPrincipal();

    /// <inheritdoc/>
    public IDictionary<object, object?> Items => _items;

    /// <inheritdoc/>
    public bool IsHttps => context.Request.IsSecureConnection;

    /// <inheritdoc/>
    public string? ContentType
    {
        get => context.Response.ContentType;
        set => context.Response.ContentType = value;
    }

    /// <inheritdoc/>
    public int StatusCode
    {
        get => context.Response.StatusCode;
        set => context.Response.StatusCode = value;
    }

    JsonSerializerOptions JsonSerializerOptions => _jsonOptions ??= RequestServices.GetRequiredService<IOptions<ArcOptions>>().Value.JsonSerializerOptions;

    /// <inheritdoc/>
    public async Task<object?> ReadBodyAsJsonAsync(Type type, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
        var json = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }
        return JsonSerializer.Deserialize(json, type, JsonSerializerOptions);
    }

    /// <inheritdoc/>
    public void SetStatusCode(int statusCode)
    {
        context.Response.StatusCode = statusCode;
    }

    /// <inheritdoc/>
    public void SetResponseHeader(string name, string value)
    {
        context.Response.Headers[name] = value;
    }

    /// <inheritdoc/>
    public async Task WriteResponseAsJsonAsync(object? value, Type type, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        context.Response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(value, type, JsonSerializerOptions);
        var buffer = Encoding.UTF8.GetBytes(json);
        await context.Response.OutputStream.WriteAsync(buffer, cancellationToken);
    }

    /// <inheritdoc/>
    public void AppendCookie(string key, string value, CookieOptions options)
    {
        var cookie = new Cookie(key, value)
        {
            HttpOnly = options.HttpOnly,
            Secure = options.Secure,
            Path = options.Path,
            Domain = options.Domain
        };

        if (options.Expires.HasValue)
        {
            cookie.Expires = options.Expires.Value.DateTime;
        }

        context.Response.Cookies.Add(cookie);
    }

    /// <inheritdoc/>
    public void RemoveCookie(string key)
    {
        var cookie = new Cookie(key, string.Empty)
        {
            Expires = DateTime.Now.AddDays(-1)
        };
        context.Response.Cookies.Add(cookie);
    }

    /// <inheritdoc/>
    public async Task WriteAsync(string text, CancellationToken cancellationToken = default)
    {
        var buffer = Encoding.UTF8.GetBytes(text);
        context.Response.ContentLength64 = buffer.Length;
        await context.Response.OutputStream.WriteAsync(buffer, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task WriteBytesAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        context.Response.ContentLength64 = data.Length;
        await context.Response.OutputStream.WriteAsync(data, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task WriteStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        context.Response.ContentLength64 = stream.Length;
        await stream.CopyToAsync(context.Response.OutputStream, cancellationToken);
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

    static ReadOnlyDictionary<string, string> ParseCookies(CookieCollection cookies)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Cookie cookie in cookies)
        {
            result[cookie.Name] = cookie.Value;
        }
        return result.AsReadOnly();
    }
}
