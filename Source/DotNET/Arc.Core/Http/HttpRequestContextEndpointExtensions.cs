// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http;

/// <summary>
/// Extension methods for exposing the current endpoint's metadata on an <see cref="IHttpRequestContext"/>.
/// </summary>
public static class HttpRequestContextEndpointExtensions
{
    const string EndpointMetadataKey = "Cratis.Arc.Http.EndpointMetadata";

    /// <summary>
    /// Sets the <see cref="EndpointMetadata"/> for the current request on the context.
    /// </summary>
    /// <param name="context">The <see cref="IHttpRequestContext"/>.</param>
    /// <param name="metadata">The <see cref="EndpointMetadata"/> for the endpoint being invoked.</param>
    public static void SetEndpointMetadata(this IHttpRequestContext context, EndpointMetadata? metadata) => context.Items[EndpointMetadataKey] = metadata;

    /// <summary>
    /// Gets the <see cref="EndpointMetadata"/> for the current request from the context, if any was set.
    /// </summary>
    /// <param name="context">The <see cref="IHttpRequestContext"/>.</param>
    /// <returns>The <see cref="EndpointMetadata"/>, or <see langword="null"/> if none was set.</returns>
    public static EndpointMetadata? GetEndpointMetadata(this IHttpRequestContext context) =>
        context.Items?.TryGetValue(EndpointMetadataKey, out var value) == true ? value as EndpointMetadata : default;

    /// <summary>
    /// Gets a value indicating whether the current request's endpoint allows anonymous access.
    /// </summary>
    /// <param name="context">The <see cref="IHttpRequestContext"/>.</param>
    /// <returns>True if the endpoint allows anonymous access, false otherwise.</returns>
    public static bool AllowsAnonymous(this IHttpRequestContext context) => context.GetEndpointMetadata()?.AllowAnonymous == true;
}
