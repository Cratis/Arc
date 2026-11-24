// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_graph_events_span_slices : given.a_generated_document
{
    const string Created = """
        using Cratis.Chronicle.Events;
        namespace Library.People.Added;
        [EventType("created")] public record Created(string Id);
        """;
    const string Removed = """
        using Cratis.Chronicle.Events;
        namespace Library.People.Deleted;
        [EventType("removed")] public record Removed(string Id);
        """;
    const string Graph = """
        using Cratis.Chronicle.Keys;
        using Cratis.Chronicle.Projections.Graphs;
        using Cratis.Chronicle.Projections.ModelBound;
        using Library.People.Added;
        using Library.People.Deleted;
        namespace Library.Graphs;
        [Graph("People"), Node, FromEvent<Created>, RemovedWith<Removed>]
        public record Person([Key] string Id);
        """;

    void Because() => Generate(("Library/Added/Created.cs", Created), ("Library/Deleted/Removed.cs", Removed), ("Library/Graphs/Graph.cs", Graph));

    [Fact] void should_bind_all_event_references() => AssertDocument();

    /// <summary>Ordinal-first placement is Arc's canonical reconstruction, never recovered Stage ownership.</summary>
    [Fact] void should_use_the_ordinal_first_event_slice_name() => Bound.Value.Model.Application.GraphProjections.Single().Name.ShouldEqual("Added");
    [Fact] void should_keep_a_single_projection() => Bound.Value.Model.Application.GraphProjections.Length.ShouldEqual(1);
    [Fact] void should_report_the_fallback() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.CanonicalGraphPlacement).ShouldBeTrue();
}
