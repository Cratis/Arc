// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_graph_behavior_is_custom_code : given.a_generated_document
{
    void Because() => Generate(("Library/Graphs/Custom.cs", """
        using Cratis.Chronicle.Projections.Graphs;
        namespace Library.Graphs;
        public class Custom : IGraphProjection
        {
            public void Define(IGraphProjectionBuilder builder) { }
        }
        """));

    [Fact] void should_not_execute_or_invent_the_graph() => AssertPartialDocument();
    [Fact] void should_report_unrecoverable_behavior() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrecoverableGraph).ShouldBeTrue();
    [Fact] void should_not_emit_a_projection() => Bound.Value.Model.Application.GraphProjections.ShouldBeEmpty();
}
