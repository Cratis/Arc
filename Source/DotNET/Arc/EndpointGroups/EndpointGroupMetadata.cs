// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.AspNetCore.EndpointGroups;

/// <summary>
/// Endpoint metadata naming the endpoint group an Arc endpoint belongs to.
/// </summary>
/// <param name="Name">The name of the group, or <see langword="null"/> for the default group.</param>
/// <remarks>
/// <see cref="EndpointGroupMatcherPolicy"/> uses it to select endpoints by the local port of the connection.
/// Endpoints without this metadata are not Arc endpoints and are left alone.
/// </remarks>
public sealed record EndpointGroupMetadata(string? Name);
