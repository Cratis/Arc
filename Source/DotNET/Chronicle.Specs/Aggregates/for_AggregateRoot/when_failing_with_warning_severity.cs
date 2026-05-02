// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRoot;

public class when_failing_with_warning_severity : given.an_aggregate_root
{
    AggregateRootCommitResult _expectedResult;
    AggregateRootCommitResult _result;

    void Establish()
    {
        _expectedResult = AggregateRootCommitResult.Successful();
        _mutation.Commit().Returns(Task.FromResult(_expectedResult));
    }

    async Task Because()
    {
        _aggregateRoot.ReportFailed("A warning occurred", ValidationResultSeverity.Warning);
        _result = await _aggregateRoot.Commit();
    }

    [Fact] void should_call_commit_on_mutation() => _mutation.Received(1).Commit();
    [Fact] void should_dehydrate_mutator() => _mutator.Received(1).Dehydrate();
    [Fact] void should_return_a_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_hold_one_validation_result() => _result.ValidationResults.Count().ShouldEqual(1);
    [Fact] void should_hold_validation_result_with_warning_severity() => _result.ValidationResults.First().Severity.ShouldEqual(ValidationResultSeverity.Warning);
    [Fact] void should_hold_validation_result_with_message() => _result.ValidationResults.First().Message.ShouldEqual("A warning occurred");
}
