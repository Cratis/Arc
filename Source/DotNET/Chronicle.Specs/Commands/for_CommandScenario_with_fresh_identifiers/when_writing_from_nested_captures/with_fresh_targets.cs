// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_writing_from_nested_captures;

public class with_fresh_targets : given.a_nested_capture_matrix
{
    async Task Because() => await ExecuteAll();

    [Fact] void should_exercise_all_three_attributes_and_the_aggregate() => _outcomes.Count.ShouldEqual(4);
    [Fact] void should_replace_derived_scopes_with_the_outer_guard() => _outcomes.ShouldEachConformTo(outcome => outcome.Result.IsSuccess);
    [Fact] void should_commit_each_nested_event() => _outcomes.ShouldEachConformTo(outcome => outcome.After == EventSequenceNumber.First);
    [Fact] void should_run_each_outer_handler_once() => _outcomes.ShouldEachConformTo(outcome => outcome.Calls == 1);
}
