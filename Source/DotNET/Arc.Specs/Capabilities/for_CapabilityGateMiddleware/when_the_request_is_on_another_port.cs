// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities.for_CapabilityGateMiddleware;

public class when_the_request_is_on_another_port : given.a_capability_gate
{
    void Establish() => _context.Connection.LocalPort = 42002;
    Task Because() => Invoke();
    [Fact] void should_continue() => _nextRan.ShouldBeTrue();
    [Fact] void should_not_resolve() => _verified.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_have_no_caller() => _caller.ShouldBeNull();
}
