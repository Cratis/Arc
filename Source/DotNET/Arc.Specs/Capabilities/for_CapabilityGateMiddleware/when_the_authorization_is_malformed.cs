// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Capabilities.for_CapabilityGateMiddleware;

public class when_the_authorization_is_malformed : given.a_capability_gate
{
    void Establish() => _verified.GetCurrent().Returns(new CallerResolution(null, CallerRefusal.MalformedAuthorization));
    Task Because() => Invoke();
    [Fact] void should_return_bad_request() => _context.Response.StatusCode.ShouldEqual(400);
    [Fact] void should_not_challenge() => _context.Response.Headers.WWWAuthenticate.Count.ShouldEqual(0);
    [Fact] void should_not_run_handler() => _nextRan.ShouldBeFalse();
}
