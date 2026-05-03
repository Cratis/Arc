// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRoot;

public class when_failing_with_error_severity : given.an_aggregate_root
{
    AggregateRootCommitResult _result;

    async Task Because()
    {
        _aggregateRoot.ReportFailed("Something went wrong");
        _result = await _aggregateRoot.Commit();
    }

    [Fact] void should_not_call_commit_on_mutation() => _mutation.DidNotReceive().Commit();
    [Fact] void should_return_a_failed_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_hold_one_validation_result() => _result.ValidationResults.Count().ShouldEqual(1);
    [Fact] void should_hold_validation_result_with_error_severity() => _result.ValidationResults.First().Severity.ShouldEqual(ValidationResultSeverity.Error);
    [Fact] void should_hold_validation_result_with_message() => _result.ValidationResults.First().Message.ShouldEqual("Something went wrong");
}
