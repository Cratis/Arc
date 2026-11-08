// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Capabilities.for_CapabilityGatedGroupPort;

public class when_an_anonymous_caller_requests_a_default_group_endpoint : given.a_routed_capability_pipeline
{
    Task Because() => Send("/default");
    [Fact] void should_respond_unauthorized() => _context.Response.StatusCode.ShouldEqual(StatusCodes.Status401Unauthorized);
    [Fact] void should_never_invoke_the_handler() => _defaultGroupHandlerCalls.ShouldEqual(0);
}
