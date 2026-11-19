// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.Filters.for_AvailabilityFilter;

public class when_the_decision_is_unknown : given.a_command_filter_with_a_gate
{
    void Establish() =>
        _gate.Decide(typeof(given.a_command_filter_with_a_gate.GatedCommand), _services, Arg.Any<CancellationToken>()).Returns(new AvailabilityDecision(AvailabilityVerdict.Unknown, ["for Plus"]));

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_carry_the_not_available_reason() => _result.AuthorizationFailureReason.ShouldEqual(AvailabilityReason.NotAvailable);
}
