// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_writing_directly;

public class with_occupied_targets : given.a_direct_write_matrix
{
    async Task Because() => await ExecuteAll(occupied: true);

    [Fact] void should_exercise_all_seven_apis() => _outcomes.Count.ShouldEqual(7);
    [Fact] void should_reject_every_api() => _outcomes.ShouldEachConformTo(outcome => !outcome.Result.IsSuccess);
    [Fact] void should_report_concurrency_violations() => _outcomes.ShouldEachConformTo(outcome => outcome.Result.ValidationResults.Single().Reason == ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_classify_generated_collisions() => _outcomes.ShouldEachConformTo(outcome => outcome.Result.ValidationResults.Single().ReasonDetail == "GeneratedIdentifierCollision");
    [Fact] void should_append_nothing() => _outcomes.ShouldEachConformTo(outcome => outcome.After == outcome.Before);
    [Fact] void should_not_retry_handlers() => _outcomes.ShouldEachConformTo(outcome => outcome.Calls == 1);
}
