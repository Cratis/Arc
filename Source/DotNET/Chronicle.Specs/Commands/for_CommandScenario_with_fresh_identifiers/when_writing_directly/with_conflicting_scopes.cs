// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_writing_directly;

public class with_conflicting_scopes : given.a_direct_write_matrix
{
    async Task Because() => await ExecuteAll(conflict: true);

    [Fact] void should_exercise_all_seven_apis() => _outcomes.Count.ShouldEqual(7);
    [Fact] void should_reject_every_scope() => _outcomes.ShouldEachConformTo(outcome => !outcome.Result.IsSuccess);
    [Fact] void should_report_arcs_conflict() => _outcomes.ShouldEachConformTo(outcome => outcome.Result.ExceptionMessages.Any(message => message.Contains(new ConflictingConcurrencyScopes().Message, StringComparison.Ordinal)));
    [Fact] void should_redact_the_identifier() => _outcomes.ShouldEachConformTo(outcome => outcome.Result.ExceptionMessages.All(message => !message.Contains(_id.Value.ToString(), StringComparison.Ordinal)));
    [Fact] void should_stage_nothing() => _outcomes.ShouldEachConformTo(outcome => outcome.After == outcome.Before);
}
