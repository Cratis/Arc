// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;

namespace Cratis.Arc.Commands.Filters.for_AvailabilityFilter;

public class when_the_command_is_suspended : given.a_command_filter_with_a_gate
{
    void Establish() =>
        _gate.Decide(typeof(given.a_command_filter_with_a_gate.GatedCommand), _services, Arg.Any<CancellationToken>()).Returns(new AvailabilityDecision(AvailabilityVerdict.Suspended, ["Suspended"]));

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_carry_the_suspended_reason() => _result.AuthorizationFailureReason.ShouldEqual(AvailabilityReason.Suspended);
    [Fact] void should_not_carry_the_not_available_reason() => _result.AuthorizationFailureReason.ShouldNotEqual(AvailabilityReason.NotAvailable);
    [Fact] void should_not_be_retryable() => _result.IsRetryable.ShouldBeFalse();
}
