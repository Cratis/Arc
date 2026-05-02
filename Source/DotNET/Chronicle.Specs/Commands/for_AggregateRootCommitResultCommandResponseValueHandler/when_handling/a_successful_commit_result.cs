// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_AggregateRootCommitResultCommandResponseValueHandler.when_handling;

public class a_successful_commit_result : given.a_handler
{
    AggregateRootCommitResult _commitResult;
    CommandResult _commandResult;

    void Establish() =>
        _commitResult = AggregateRootCommitResult.Successful();

    async Task Because() => _commandResult = await _handler.Handle(_commandContext, _commitResult);

    [Fact] void should_return_successful_command_result() => _commandResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_correct_correlation_id() => _commandResult.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_have_no_validation_results() => _commandResult.ValidationResults.ShouldBeEmpty();
}
