// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.EndpointGroups;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

[NonController]
[ApiController]
[EndpointGroup("capabilities")]
[RequiresScope("content:write")]
[Route("scoped-controller-spec")]
public class ScopedProbeController(ScopedControllerProbe probe) : ControllerBase
{
    [HttpGet]
    public string Read()
    {
        probe.Ran();
        return "read";
    }

    [HttpPost]
    public string Write()
    {
        probe.Ran();
        return "write";
    }
}
