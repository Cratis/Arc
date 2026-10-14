// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_writing_from_nested_captures;

public class with_occupied_targets : given.a_nested_capture_matrix
{
    async Task Because() => await ExecuteAll(occupied: true);

    [Fact] void should_exercise_all_three_attributes_and_the_aggregate() => _outcomes.Count.ShouldEqual(4);
    [Fact] void should_reject_each_occupied_target() => _outcomes.ShouldEachConformTo(outcome => !outcome.Result.IsSuccess);
    [Fact] void should_report_concurrency_violations() => _outcomes.ShouldEachConformTo(outcome => outcome.Result.ValidationResults.Single().Reason == ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_classify_each_outer_capture_collision() => _outcomes.ShouldEachConformTo(outcome => outcome.Result.ValidationResults.Single().ReasonDetail == "GeneratedIdentifierCollision");
    [Fact] void should_append_nothing() => _outcomes.ShouldEachConformTo(outcome => outcome.After == outcome.Before);
    [Fact] void should_not_retry_the_outer_handler() => _outcomes.ShouldEachConformTo(outcome => outcome.Calls == 1);
}
