// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http;

/// <summary>
/// Represents information about a registered route.
/// </summary>
/// <param name="Method">The HTTP method (GET, POST, etc.).</param>
/// <param name="Pattern">The route pattern.</param>
/// <param name="Metadata">The endpoint metadata.</param>
public record RouteInfo(string Method, string Pattern, EndpointMetadata? Metadata);
