// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Capabilities.for_CapabilityGatedGroupPort;

public class when_a_verified_caller_requests_a_capability_group_endpoint : given.a_routed_capability_pipeline
{
    void Establish() => CallerIsVerified();
    Task Because() => Send("/capability");
    [Fact] void should_respond_ok() => _context.Response.StatusCode.ShouldEqual(StatusCodes.Status200OK);
    [Fact] void should_invoke_the_handler() => _capabilityGroupHandlerCalls.ShouldEqual(1);
}
