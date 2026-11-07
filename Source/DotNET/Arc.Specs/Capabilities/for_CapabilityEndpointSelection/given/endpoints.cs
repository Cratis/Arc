// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Cratis.Arc.Capabilities.for_CapabilityEndpointSelection.given;

public static class endpoints
{
    public static RouteEndpoint Route(string pattern, params object[] metadata) =>
        new(_ => Task.CompletedTask, RoutePatternFactory.Parse(pattern), 0, new EndpointMetadataCollection(metadata), pattern);

    public class Marker;

    public class DerivedMarker : Marker;
}
