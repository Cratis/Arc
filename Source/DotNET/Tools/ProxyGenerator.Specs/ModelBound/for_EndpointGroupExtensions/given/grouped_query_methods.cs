// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_EndpointGroupExtensions.given;

[EndpointGroup("internal")]
public record grouped_query_methods
{
    public static int Inherited() => 0;

    [EndpointGroup("ops")]
    public static int Overridden() => 0;
}
