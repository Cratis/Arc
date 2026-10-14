// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_writing_directly;

public class with_equal_explicit_scopes : given.a_direct_write_matrix
{
    async Task Because() => await ExecuteAll(equalScope: true);

    [Fact] void should_exercise_all_seven_apis() => _outcomes.Count.ShouldEqual(7);
    [Fact] void should_accept_equal_scopes() => _outcomes.ShouldEachConformTo(outcome => outcome.Result.IsSuccess);
    [Fact] void should_append_both_events() => _outcomes.ShouldEachConformTo(outcome => outcome.After == EventSequenceNumber.First + 1);
}
