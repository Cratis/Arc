// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Http;

/// <summary>
/// Represents an abstraction for an HTTP request context that can be implemented by different HTTP frameworks.
/// </summary>
public interface IHttpRequestContext
{
    /// <summary>
    /// Gets the query string parameters.
    /// </summary>
    IReadOnlyDictionary<string, string> Query { get; }

    /// <summary>
    /// Gets the request headers.
    /// </summary>
    IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>
    /// Gets the request cookies.
    /// </summary>
    IReadOnlyDictionary<string, string> Cookies { get; }

    /// <summary>
    /// Gets the request path.
    /// </summary>
    string Path { get; }

    /// <summary>
    /// Gets the HTTP method (GET, POST, etc.).
    /// </summary>
    string Method { get; }

    /// <summary>
    /// Gets the service provider for the request scope.
    /// </summary>
    IServiceProvider RequestServices { get; }

    /// <summary>
    /// Gets the cancellation token for the request.
    /// </summary>
    CancellationToken RequestAborted { get; }

    /// <summary>
    /// Gets the WebSocket context for this request.
    /// </summary>
    IWebSocketContext WebSockets { get; }

    /// <summary>
    /// Gets the user associated with the request.
    /// </summary>
    ClaimsPrincipal User { get; }

    /// <summary>
    /// Gets a dictionary for storing arbitrary data during the request lifecycle.
    /// </summary>
    IDictionary<object, object?> Items { get; }

    /// <summary>
    /// Gets a value indicating whether the request is HTTPS.
    /// </summary>
    bool IsHttps { get; }

    /// <summary>
    /// Gets the content type of the response.
    /// </summary>
    string? ContentType { get; set; }

    /// <summary>
    /// Gets the status code of the response.
    /// </summary>
    int StatusCode { get; set; }

    /// <summary>
    /// Reads the request body and deserializes it as JSON.
    /// </summary>
    /// <param name="type">The type to deserialize to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized object.</returns>
    Task<object?> ReadBodyAsJsonAsync(Type type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the HTTP response status code.
    /// </summary>
    /// <param name="statusCode">The status code to set.</param>
    void SetStatusCode(int statusCode);

    /// <summary>
    /// Sets a response header.
    /// </summary>
    /// <param name="name">Header name.</param>
    /// <param name="value">Header value.</param>
    void SetResponseHeader(string name, string value);

    /// <summary>
    /// Writes JSON to the response.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="type">The type of the value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task WriteResponseAsJsonAsync(object? value, Type type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends a cookie to the response.
    /// </summary>
    /// <param name="key">The cookie name.</param>
    /// <param name="value">The cookie value.</param>
    /// <param name="options">The cookie options.</param>
    void AppendCookie(string key, string value, CookieOptions options);

    /// <summary>
    /// Removes a cookie from the response.
    /// </summary>
    /// <param name="key">The cookie name.</param>
    void RemoveCookie(string key);

    /// <summary>
    /// Writes text to the response.
    /// </summary>
    /// <param name="text">The text to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task WriteAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes bytes to the response.
    /// </summary>
    /// <param name="data">The byte data to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task WriteBytesAsync(byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a stream to the response.
    /// </summary>
    /// <param name="stream">The stream to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task WriteStreamAsync(Stream stream, CancellationToken cancellationToken = default);
}
