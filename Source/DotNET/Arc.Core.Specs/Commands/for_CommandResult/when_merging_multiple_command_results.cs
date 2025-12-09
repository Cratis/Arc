// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandResult;

public class when_merging_multiple_command_results : Specification
{
    CommandResult _result;
    CommandResult _otherResult;

    void Establish()
    {
        _result = new CommandResult
        {
            CorrelationId = CorrelationId.New(),
            ExceptionMessages = ["Exception 1"],
            ExceptionStackTrace = "Stack trace 1",
            ValidationResults = [new ValidationResult(ValidationResultSeverity.Information, "Error 1", ["Property1"], "State")]
        };

        _otherResult = new CommandResult
        {
            CorrelationId = CorrelationId.New(),
            ExceptionMessages = ["Exception 2"],
            ExceptionStackTrace = "Stack trace 2",
            ValidationResults = [new ValidationResult(ValidationResultSeverity.Information, "Error 2", ["Property2"], "State")]
        };
    }

    void Because() => _result.MergeWith(_otherResult);

    [Fact] void should_keep_original_correlation_id() => _result.CorrelationId.ShouldEqual(_result.CorrelationId);
    [Fact] void should_contain_all_exception_messages() => _result.ExceptionMessages.ShouldContainOnly("Exception 1", "Exception 2");
    [Fact] void should_contain_all_validation_results() => _result.ValidationResults.Select(v => v.Message).ShouldContainOnly("Error 1", "Error 2");
    [Fact] void should_contain_stack_trace_from_other_result() => _result.ExceptionStackTrace.ShouldEqual("Stack trace 1\nStack trace 2");
}