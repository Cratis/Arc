// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Http;

/// <summary>
/// Provides shared helpers for building endpoint routes and mapping result status codes.
/// </summary>
public static class EndpointRouteHelper
{
    /// <summary>
    /// Builds a sanitized route URL from the given endpoint location and options.
    /// </summary>
    /// <param name="options">The <see cref="ApiEndpointOptions"/>.</param>
    /// <param name="location">The namespace segments for the endpoint.</param>
    /// <param name="endpointName">The name of the endpoint (command type name or query name).</param>
    /// <param name="includeNameInRoute">Whether to include the endpoint name as the last route segment.</param>
    /// <returns>A sanitized, kebab-cased, lowercase route URL.</returns>
#pragma warning disable CA1055 // URI-like return values should not be strings
    public static string BuildRouteUrl(
        ApiEndpointOptions options,
        IEnumerable<string> location,
        string endpointName,
        bool includeNameInRoute)
    {
        var prefix = options.RoutePrefix.Trim('/');
        var segments = location.Select(segment => segment.ToKebabCase());
        var baseUrl = $"/{prefix}/{string.Join('/', segments)}";

        var typeName = includeNameInRoute ? endpointName : string.Empty;
        var url = includeNameInRoute ? $"{baseUrl}/{typeName.ToKebabCase()}" : baseUrl;

        return url.ToLowerInvariant().SanitizeUrl();
    }
#pragma warning restore CA1055 // URI-like return values should not be strings

    /// <summary>
    /// Determines whether the endpoint name should be included in the route,
    /// based on the configured option and whether there are conflicting endpoints in the same namespace.
    /// </summary>
    /// <typeparam name="T">The type of endpoint descriptor (command handler, query performer, etc.).</typeparam>
    /// <param name="includeNameOption">The configured option for including the name (e.g. <see cref="ApiEndpointOptions.IncludeCommandNameInRoute"/>).</param>
    /// <param name="location">The namespace segments for the endpoint.</param>
    /// <param name="endpointsByNamespace">The endpoints grouped by namespace key.</param>
    /// <returns>True if the endpoint name should be included in the route.</returns>
    public static bool ShouldIncludeNameInRoute<T>(
        bool includeNameOption,
        IEnumerable<string> location,
        IReadOnlyDictionary<string, List<T>> endpointsByNamespace)
    {
        var namespaceKey = string.Join('.', location);
        var hasConflict = endpointsByNamespace.TryGetValue(namespaceKey, out var endpointsInNamespace) && endpointsInNamespace.Count > 1;
        return includeNameOption || hasConflict;
    }

    /// <summary>
    /// Groups endpoint descriptors by their namespace key (location segments joined by '.').
    /// </summary>
    /// <typeparam name="T">The type of endpoint descriptor.</typeparam>
    /// <param name="endpoints">The endpoints to group.</param>
    /// <param name="locationSelector">A function to get the location segments from an endpoint.</param>
    /// <param name="segmentsToSkip">The number of leading namespace segments to skip.</param>
    /// <returns>A dictionary mapping namespace keys to lists of endpoints.</returns>
    public static IReadOnlyDictionary<string, List<T>> GroupByNamespace<T>(
        IEnumerable<T> endpoints,
        Func<T, IEnumerable<string>> locationSelector,
        int segmentsToSkip)
    {
        return endpoints
            .GroupBy(e => string.Join('.', locationSelector(e).Skip(segmentsToSkip)))
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Determines the HTTP status code for a result that has success, authorization, and validation state.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="isAuthorized">Whether the operation was authorized.</param>
    /// <param name="isValid">Whether the input was valid.</param>
    /// <returns>The appropriate <see cref="HttpStatusCode"/>.</returns>
    public static HttpStatusCode GetStatusCode(bool isSuccess, bool isAuthorized, bool isValid)
    {
        return (isSuccess, isAuthorized, isValid) switch
        {
            (true, _, _) => HttpStatusCode.OK,
            (_, false, _) => HttpStatusCode.Forbidden,
            (_, _, false) => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };
    }
}
