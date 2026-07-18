// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandResult.when_creating_from_an_exception;

public class and_the_exception_is_a_plain_error : Specification
{
    CorrelationId _correlationId;
    CommandResult _result;

    void Establish() => _correlationId = CorrelationId.New();

    void Because() => _result = CommandResult.FromException(_correlationId, new InvalidOperationException("boom"));

    [Fact] void should_carry_the_exception() => _result.HasExceptions.ShouldBeTrue();
    [Fact] void should_remain_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_keep_the_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}
