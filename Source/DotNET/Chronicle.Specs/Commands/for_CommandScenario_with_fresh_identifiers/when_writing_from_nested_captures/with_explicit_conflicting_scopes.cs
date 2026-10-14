// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_writing_from_nested_captures;

public class with_explicit_conflicting_scopes : given.a_nested_capture_matrix
{
    async Task Because() => await ExecuteAll(explicitScopes: true);

    [Fact] void should_exercise_returned_and_direct_explicit_scopes() => _outcomes.Count.ShouldEqual(2);
    [Fact] void should_keep_explicit_scope_validation_strict() => _outcomes.ShouldEachConformTo(outcome => !outcome.Result.IsSuccess);
    [Fact] void should_report_arcs_redacted_conflict() => _outcomes.ShouldEachConformTo(outcome => outcome.NestedResult.ExceptionMessages.Contains(new ConflictingConcurrencyScopes().Message));
    [Fact] void should_stage_nothing() => _outcomes.ShouldEachConformTo(outcome => outcome.After == outcome.Before);
}
