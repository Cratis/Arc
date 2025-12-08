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
public record EndpointMetadata(
    string Name,
    string? Summary = default,
    IEnumerable<string>? Tags = default,
    bool AllowAnonymous = false);
