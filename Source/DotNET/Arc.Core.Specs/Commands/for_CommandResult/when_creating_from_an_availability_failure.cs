// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandResult;

public class when_creating_from_an_availability_failure : Specification
{
    CommandResult _result;
    CommandResult _merged;

    void Because()
    {
        _result = CommandResult.FromException(CorrelationId.New(), new AvailabilityNotDecidedException(new InvalidOperationException("store is down")));
        _merged = CommandResult.Success(CorrelationId.New());
        _merged.MergeWith(_result);
    }

    [Fact] void should_be_retryable() => _result.IsRetryable.ShouldBeTrue();
    [Fact] void should_still_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_leak_the_cause() => _result.ExceptionMessages.Single().ShouldNotContain("store is down");
    [Fact] void should_stay_retryable_when_merged() => _merged.IsRetryable.ShouldBeTrue();
}
