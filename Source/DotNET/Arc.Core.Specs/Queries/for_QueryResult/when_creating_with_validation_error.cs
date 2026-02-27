// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_QueryResult;

public class when_creating_with_validation_error : Specification
{
    CorrelationId _correlationId;
    QueryResult _result;

    void Establish() => _correlationId = CorrelationId.New();

    void Because() => _result = QueryResult.WithValidationError(_correlationId, "myParam", "myParam is required");

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_have_one_validation_result() => _result.ValidationResults.Count().ShouldEqual(1);
    [Fact] void should_have_validation_result_with_error_severity() => _result.ValidationResults.First().Severity.ShouldEqual(ValidationResultSeverity.Error);
    [Fact] void should_have_validation_result_with_correct_member() => _result.ValidationResults.First().Members.ShouldContain("myParam");
    [Fact] void should_have_validation_result_with_correct_message() => _result.ValidationResults.First().Message.ShouldEqual("myParam is required");
    [Fact] void should_have_correct_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}
