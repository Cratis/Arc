// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRoot;

public class when_failing_with_multiple_errors : given.an_aggregate_root
{
    AggregateRootCommitResult _result;

    async Task Because()
    {
        _aggregateRoot.ReportFailed("First error");
        _aggregateRoot.ReportFailed("Second error", ValidationResultSeverity.Warning);
        _aggregateRoot.ReportFailed("Third error");
        _result = await _aggregateRoot.Commit();
    }

    [Fact] void should_not_call_commit_on_mutation() => _mutation.DidNotReceive().Commit();
    [Fact] void should_return_a_failed_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_hold_three_validation_results() => _result.ValidationResults.Count().ShouldEqual(3);
    [Fact] void should_include_all_messages() =>
        _result.ValidationResults.Select(v => v.Message).ShouldContainOnly(["First error", "Second error", "Third error"]);
}
