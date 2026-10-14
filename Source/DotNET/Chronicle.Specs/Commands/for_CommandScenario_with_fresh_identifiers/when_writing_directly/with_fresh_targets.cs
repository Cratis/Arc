// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_writing_directly;

public class with_fresh_targets : given.a_direct_write_matrix
{
    async Task Because() => await ExecuteAll();

    [Fact] void should_exercise_all_seven_apis() => _outcomes.Count.ShouldEqual(7);
    [Fact] void should_succeed_for_every_api() => _outcomes.ShouldEachConformTo(outcome => outcome.Result.IsSuccess);
    [Fact] void should_append_both_events_for_each_api() => _outcomes.ShouldEachConformTo(outcome => outcome.After == EventSequenceNumber.First + 1);
    [Fact] void should_invoke_each_handler_once() => _outcomes.ShouldEachConformTo(outcome => outcome.Calls == 1);
}
