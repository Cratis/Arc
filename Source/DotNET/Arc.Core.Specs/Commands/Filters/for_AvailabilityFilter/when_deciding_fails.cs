// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.Filters.for_AvailabilityFilter;

public class when_deciding_fails : given.a_command_filter_with_a_gate
{
    void Establish() =>
        _gate.Decide(typeof(given.a_command_filter_with_a_gate.GatedCommand), _services, Arg.Any<CancellationToken>()).Returns<ValueTask<AvailabilityDecision>>(_ => throw new InvalidOperationException("store is down"));

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_still_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_not_carry_the_not_available_reason() => _result.AuthorizationFailureReason.ShouldBeEmpty();
    [Fact] void should_be_a_retryable_error() => _result.IsRetryable.ShouldBeTrue();
    [Fact] void should_have_an_exception_message() => _result.HasExceptions.ShouldBeTrue();
    [Fact] void should_not_leak_the_cause() => _result.ExceptionMessages.Single().ShouldNotContain("store is down");
}
