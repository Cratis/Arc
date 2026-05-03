// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_AggregateRootCommitResultExtensions.when_converting;

public class a_successful_result : given.all_dependencies
{
    AggregateRootCommitResult _commitResult;
    CommandResult _result;

    void Establish() =>
        _commitResult = AggregateRootCommitResult.Successful();

    void Because() => _result = _commitResult.ToCommandResult(_correlationId);

    [Fact] void should_return_successful_command_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_correct_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_have_no_validation_results() => _result.ValidationResults.ShouldBeEmpty();
    [Fact] void should_have_no_exception_messages() => _result.ExceptionMessages.ShouldBeEmpty();
}
