// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

[Collection("UsesCurrentDirectory")]
public class when_reexecuting_from_outside_the_gate : given.a_reexecuting_capability_host
{
    int _statusReexecution;
    int _exceptionReexecution;
    int _rewrite;
    int _unknownEndpoint;

    async Task Because()
    {
        _statusReexecution = await Request("/legacy/status");
        _exceptionReexecution = await Request("/legacy/exception");
        _rewrite = await Request("/legacy/rewrite");
        _unknownEndpoint = await Request("/legacy/unknown");
    }

    [Fact] void should_refuse_status_code_reexecution_to_a_selected_endpoint() => _statusReexecution.ShouldEqual(401);
    [Fact] void should_refuse_exception_reexecution_to_a_selected_endpoint() => _exceptionReexecution.ShouldEqual(401);
    [Fact] void should_refuse_clearing_and_rerouting_to_a_selected_endpoint() => _rewrite.ShouldEqual(401);
    [Fact] void should_refuse_a_runtime_endpoint_assignment() => _unknownEndpoint.ShouldEqual(401);
    [Fact] void should_never_execute_the_selected_or_unknown_handler() => _selectedCalls.ShouldEqual(0);
}
