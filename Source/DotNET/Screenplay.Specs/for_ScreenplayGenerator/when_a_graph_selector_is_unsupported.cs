// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_a_graph_selector_is_unsupported : given.a_generated_document
{
    void Because() => Generate(given.GraphSources.Files(given.GraphSources.Graph.Replace("nameof(EmploymentStarted.PersonId)", "\"$unsupported(PersonId)\"", StringComparison.Ordinal)));

    [Fact] void should_keep_the_supported_shape_compilable() => AssertPartialDocument();
    [Fact] void should_retain_the_graph_shape() => Bound.Value.Model.Application.Graphs.Length.ShouldEqual(1);
    [Fact] void should_withhold_the_projection_instead_of_creating_an_event_property_path() => Bound.Value.Model.Application.GraphProjections.ShouldBeEmpty();
    [Fact] void should_diagnose_the_unsupported_expression() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrecoverableGraph).ShouldBeTrue();
}
