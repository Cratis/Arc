// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.Queries.for_WebSocketOriginGate;

public record OriginHubReadModel
{
    [Path("/origin-probe/hub")]
    public static int Query(OriginProbe probe)
    {
        probe.Ran();
        return 1;
    }
}
