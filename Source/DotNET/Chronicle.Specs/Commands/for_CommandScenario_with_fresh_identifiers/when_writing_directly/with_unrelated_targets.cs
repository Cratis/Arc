// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_writing_directly;

public class with_unrelated_targets : given.a_direct_write_matrix
{
    async Task Because() => await ExecuteAll(occupied: true, unrelated: true);

    [Fact] void should_exercise_all_seven_apis() => _outcomes.Count.ShouldEqual(7);
    [Fact] void should_not_guard_unrelated_sources() => _outcomes.ShouldEachConformTo(outcome => outcome.Result.IsSuccess);
    [Fact] void should_append_to_the_unrelated_source() => _outcomes.ShouldEachConformTo(outcome => outcome.After == outcome.Before + 2);
}
