// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandResult.when_creating_from_an_exception;

public class and_the_exception_is_a_validation_failure : Specification
{
    CorrelationId _correlationId;
    CommandResult _result;

    void Establish() => _correlationId = CorrelationId.New();

    void Because() => _result = CommandResult.FromException(_correlationId, new TheValidationFailure());

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_carry_any_exception_detail() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_surface_the_validation_message() => _result.ValidationResults.Single().Message.ShouldEqual("invalid input");
    [Fact] void should_keep_the_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);

    class TheValidationFailure : Exception, IValidationFailure
    {
        public ValidationResult ValidationResult { get; } = ValidationResult.Error("invalid input");
    }
}
