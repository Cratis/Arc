// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_EndpointGroupExtensions.given;

public record query_methods
{
    public static int Public() => 0;

    [EndpointGroup("internal")]
    public static int Internal() => 0;
}
