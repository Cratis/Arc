// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Queries.for_WebSocketOriginGate;

[ApiController]
public class OriginProbeController(OriginProbe probe) : ControllerBase
{
    [HttpGet("/origin-probe/stream")]
    public ISubject<int> Stream()
    {
        probe.Ran();
        return new BehaviorSubject<int>(1);
    }
}
