// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.AspNetCore.EndpointGroups.for_EndpointGroupListenerValidation.given;

[ApiController]
[Route("endpoint-group-startup")]
public class ActionGroupedController : ControllerBase
{
    [HttpGet]
    [EndpointGroup("internal")]
    public string Query() => "internal";
}
