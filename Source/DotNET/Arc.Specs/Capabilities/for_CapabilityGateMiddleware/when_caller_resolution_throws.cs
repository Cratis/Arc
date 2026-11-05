// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Capabilities.for_CapabilityGateMiddleware;

public class when_caller_resolution_throws : given.a_capability_gate
{
    Exception? _exception;
    void Establish() => _verified.GetCurrent().Returns(Task.FromException<CallerResolution>(new InvalidOperationException("fixture")));
    async Task Because()
    {
        try { await Invoke(); }
        catch (Exception exception) { _exception = exception; }
    }
    [Fact] void should_propagate() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_run_handler() => _nextRan.ShouldBeFalse();
}
