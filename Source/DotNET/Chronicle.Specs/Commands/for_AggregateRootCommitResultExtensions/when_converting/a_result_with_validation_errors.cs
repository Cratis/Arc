// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.Commands.for_AggregateRootCommitResultExtensions.when_converting;

public class a_result_with_validation_errors : given.all_dependencies
{
    AggregateRootCommitResult _commitResult;
    CommandResult _result;
    ValidationResult _validationResult;

    void Establish()
    {
        _validationResult = ValidationResult.Error("Something is wrong");
        _commitResult = AggregateRootCommitResult.WithErrors([_validationResult]);
    }

    void Because() => _result = _commitResult.ToCommandResult(_correlationId);

    [Fact] void should_return_failed_command_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_correct_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_have_one_validation_result() => _result.ValidationResults.Count().ShouldEqual(1);
    [Fact] void should_include_validation_error_message() => _result.ValidationResults.First().Message.ShouldEqual("Something is wrong");
    [Fact] void should_include_error_severity() => _result.ValidationResults.First().Severity.ShouldEqual(ValidationResultSeverity.Error);
}
