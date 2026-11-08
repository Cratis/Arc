// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Capabilities.for_CapabilityGatedGroupPort;

public class when_a_verified_caller_requests_a_default_group_endpoint : given.a_routed_capability_pipeline
{
    void Establish() => CallerIsVerified();
    Task Because() => Send("/default");
    [Fact] void should_respond_not_found() => _context.Response.StatusCode.ShouldEqual(StatusCodes.Status404NotFound);
    [Fact] void should_never_invoke_the_handler() => _defaultGroupHandlerCalls.ShouldEqual(0);
}
