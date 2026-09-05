// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a reader that turns an incoming HTTP request into a parsed <see cref="QueryRequest"/> for a specific HTTP method.
/// </summary>
/// <remarks>
/// This is the extension point for query transports. Adding support for a new HTTP method or argument
/// source is done by adding a new implementation — discovered automatically via
/// <see cref="Cratis.Types.IInstancesOf{T}"/> — without changing the endpoint mapper.
/// </remarks>
public interface IQueryRequestReader
{
    /// <summary>
    /// Gets the HTTP method this reader handles (e.g. GET, QUERY).
    /// </summary>
    string HttpMethod { get; }

    /// <summary>
    /// Gets the prefix used when naming the endpoint, keeping endpoint names unique across transports.
    /// </summary>
    string EndpointNamePrefix { get; }

    /// <summary>
    /// Gets the type of the request body this reader expects, or <see langword="null"/> when the request has no body.
    /// </summary>
    Type? RequestBodyType { get; }

    /// <summary>
    /// Gets a value indicating whether endpoints served by this reader should be included in the generated API description.
    /// </summary>
    bool IncludeInApiDescription { get; }

    /// <summary>
    /// Gets the value to set on the response <c>Cache-Control</c> header, or <see langword="null"/> to leave it unset.
    /// </summary>
    string? ResponseCacheControl { get; }

    /// <summary>
    /// Reads the <see cref="QueryRequest"/> from the given request context.
    /// </summary>
    /// <param name="context">The <see cref="IHttpRequestContext"/> to read from.</param>
    /// <param name="performer">The <see cref="IQueryPerformer"/> the request targets, used to type-convert arguments.</param>
    /// <returns>The parsed <see cref="QueryRequest"/>.</returns>
    Task<QueryRequest> Read(IHttpRequestContext context, IQueryPerformer performer);
}
