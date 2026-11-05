// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Capabilities.for_CapabilityGateMiddleware;

public class when_the_principal_kind_is_not_allowed : given.a_capability_gate
{
    void Establish() => _verified.GetCurrent().Returns(new CallerResolution(_person with { PrincipalKind = CallerPrincipalKind.Runner }, null));
    Task Because() => Invoke();
    [Fact] void should_forbid() => _context.Response.StatusCode.ShouldEqual(403);
    [Fact] void should_not_run_handler() => _nextRan.ShouldBeFalse();
    [Fact] void should_have_no_caller() => _callers.Current.ShouldBeNull();
}
