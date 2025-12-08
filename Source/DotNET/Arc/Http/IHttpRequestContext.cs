// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
}
