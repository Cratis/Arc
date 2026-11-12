// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.Filters.for_AvailabilityFilter;

public class when_the_command_is_available : given.a_command_filter_with_a_gate
{
    void Establish() =>
        _gate.Decide(typeof(given.a_command_filter_with_a_gate.GatedCommand), _services, Arg.Any<CancellationToken>()).Returns(AvailabilityDecision.Available);

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
