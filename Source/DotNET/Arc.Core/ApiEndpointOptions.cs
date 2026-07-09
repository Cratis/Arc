// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Represents the options for API endpoints.
/// </summary>
public class ApiEndpointOptions
{
    /// <summary>
    /// Gets or sets the route prefix to use for endpoints.
    /// </summary>
    public string RoutePrefix { get; set; } = "api";

    /// <summary>
    /// Number of segments to skip from the start of the type's namespace when constructing the route.
    /// </summary>
    public int SegmentsToSkipForRoute { get; set; }

    /// <summary>
    /// Whether to include the command name as the last segment of the route.
    /// </summary>
    public bool IncludeCommandNameInRoute { get; set; } = true;

    /// <summary>
    /// Whether to include the query name as the last segment of the route.
    /// </summary>
    public bool IncludeQueryNameInRoute { get; set; } = true;

    /// <summary>
    /// Whether to expose queries over the HTTP QUERY method (RFC 10008) in addition to GET.
    /// </summary>
    /// <remarks>
    /// When enabled (the default), every generated query endpoint also accepts the QUERY method,
    /// carrying its arguments in a JSON request body instead of the query string. Disable this to
    /// restrict query endpoints to GET only — for example behind infrastructure that rejects
    /// unknown HTTP verbs.
    /// </remarks>
    public bool EnableQueryHttpMethod { get; set; } = true;
}