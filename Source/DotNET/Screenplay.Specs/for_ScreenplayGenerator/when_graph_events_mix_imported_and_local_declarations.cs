// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_graph_events_mix_imported_and_local_declarations : Specification
{
    const string Imported = """
        using Cratis.Chronicle.Events;
        namespace External.People.Registered;
        [EventType("Registered")] public record Registered(string Id);
        """;
    const string Local = """
        using Cratis.Chronicle.Events;
        namespace Library.People.Changes;
        [EventType("LocalRemoved")] public record LocalRemoved(string Id);
        """;
    const string Graph = """
        using Cratis.Chronicle.Keys;
        using Cratis.Chronicle.Projections.Graphs;
        using Cratis.Chronicle.Projections.ModelBound;
        using External.People.Registered;
        using Library.People.Changes;
        namespace Library.Graphs;
        [Graph("People"), Node, FromEvent<Registered>, RemovedWith<LocalRemoved>]
        public record Person([Key] string Id);
        """;

    ApplicationModelAnalysis _analysis;

    void Because() => _analysis = Analyzed.SourceReferencing(Analyzed.Package("External", Imported), ("Library/Graphs/People.cs", Graph), ("Library/People/Changes/Local.cs", Local));

    [Fact] void should_place_the_projection_with_the_local_event_even_when_the_import_sorts_first() => _analysis.Model.Graphs.Single().Namespace.ShouldEqual("Library.People.Changes");
    [Fact] void should_have_a_supported_declaring_slice() => _analysis.Model.Graphs.Single().HasProjectionPlacement.ShouldBeTrue();
    [Fact] void should_keep_both_handlers() => _analysis.Model.Graphs.Single().Elements.Single().Handlers.Count.ShouldEqual(2);
    [Fact] void should_preserve_the_import() => _analysis.Model.Imports.ShouldContain("External.People.Registered.Registered");
    [Fact] void should_not_count_the_import_as_another_declaring_slice() => _analysis.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.CanonicalGraphPlacement).ShouldBeFalse();
}
