// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_an_edge_mapping_has_no_handler : given.a_generated_document
{
    void Because() => Generate(given.GraphSources.Files(given.GraphSources.Graph.Replace(
        "[SetFrom<EmploymentStarted>(nameof(EmploymentStarted.Salary))]",
        "[SetFrom<PersonRegistered>(nameof(PersonRegistered.Name))]",
        StringComparison.Ordinal)));

    [Fact] void should_keep_the_supported_document_compilable() => AssertPartialDocument();
    [Fact] void should_not_silently_emit_a_weakened_graph() => Bound.Value.Model.Application.Graphs.ShouldBeEmpty();
    [Fact] void should_report_the_unmatched_edge_mapping() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrecoverableGraph).ShouldBeTrue();
}
