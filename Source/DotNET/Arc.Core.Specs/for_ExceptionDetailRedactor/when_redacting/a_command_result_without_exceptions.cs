// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.for_ExceptionDetailRedactor.when_redacting;

public class a_command_result_without_exceptions : Specification
{
    RecordingLogger _logger;
    CommandResult _result;
    ValidationResult _validationResult;

    void Establish()
    {
        _logger = new RecordingLogger();
        _validationResult = ValidationResult.Error("Name is required", ["Name"]);
        _result = new CommandResult
        {
            CorrelationId = CorrelationId.New(),
            ValidationResults = [_validationResult]
        };
    }

    void Because() => ExceptionDetailRedactor.Redact(_result, exposeExceptionDetails: false, _logger);

    [Fact] void should_leave_the_validation_results_untouched() => _result.ValidationResults.ShouldContainOnly(_validationResult);
    [Fact] void should_not_introduce_any_exception_messages() => _result.ExceptionMessages.ShouldBeEmpty();
    [Fact] void should_not_log_anything() => _logger.Messages.ShouldBeEmpty();
}
