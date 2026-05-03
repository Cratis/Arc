// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.Commands.for_AggregateRootCommitResultCommandResponseValueHandler.when_handling;

public class a_failed_commit_result : given.a_handler
{
    AggregateRootCommitResult _commitResult;
    CommandResult _commandResult;
    ValidationResult _validationResult;

    void Establish()
    {
        _validationResult = ValidationResult.Error("Domain error");
        _commitResult = AggregateRootCommitResult.WithErrors([_validationResult]);
    }

    async Task Because() => _commandResult = await _handler.Handle(_commandContext, _commitResult);

    [Fact] void should_return_failed_command_result() => _commandResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_correct_correlation_id() => _commandResult.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_have_one_validation_result() => _commandResult.ValidationResults.Count().ShouldEqual(1);
    [Fact] void should_include_validation_error_message() => _commandResult.ValidationResults.First().Message.ShouldEqual("Domain error");
}
