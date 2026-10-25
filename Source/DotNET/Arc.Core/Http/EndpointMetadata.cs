// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http;

/// <summary>
/// Represents metadata for an endpoint.
/// </summary>
/// <param name="Name">The unique name of the endpoint.</param>
/// <param name="Summary">A summary description of what the endpoint does.</param>
/// <param name="Tags">Tags for grouping endpoints.</param>
/// <param name="AllowAnonymous">Whether anonymous access is allowed.</param>
/// <param name="RequestBodyType">The type of the request body, if any.</param>
/// <param name="ResponseType">The type of the response body, if any.</param>
/// <param name="ExcludeFromApiDescription">Whether the endpoint should be excluded from the generated API description (e.g. OpenAPI).</param>
public record EndpointMetadata(
    string Name,
    string? Summary = default,
    IEnumerable<string>? Tags = default,
    bool AllowAnonymous = false,
    Type? RequestBodyType = default,
    Type? ResponseType = default,
    bool ExcludeFromApiDescription = false)
{
    /// <summary>
    /// Gets whether this endpoint explicitly requires an authenticated caller.
    /// </summary>
    public bool RequireAuthentication { get; init; }

    /// <summary>
    /// Gets comma-separated roles, any one of which grants access to an authenticated caller.
    /// </summary>
    public string? Roles { get; init; }

    /// <summary>
    /// Gets the named endpoint group the endpoint belongs to, or <see langword="null"/> for the default group.
    /// </summary>
    /// <remarks>
    /// A host that supports endpoint groups serves the endpoint only on the local ports assigned to its group.
    /// </remarks>
    public string? EndpointGroup { get; init; }

    /// <summary>
    /// Gets a value indicating whether the endpoint is reachable on every port and resolves the artifacts it serves by
    /// the endpoint groups of the port the request arrived on, rather than belonging to one group itself.
    /// </summary>
    public bool ServesEveryEndpointGroup { get; init; }
}
