// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_graph_projection_declarations_are_invalid : given.a_generated_document
{
    static readonly string[] InvalidMarkers =
    [
        given.GraphProjectionDeclarationSources.Marker(members: "typeof(string)"),
        given.GraphProjectionDeclarationSources.Marker(members: "typeof(global::Library.People.Changes.PersonRegistered)"),
        given.GraphProjectionDeclarationSources.Marker(members: "typeof(global::Library.ReadModels.Graphs.Person)"),
        given.GraphProjectionDeclarationSources.Marker(members: given.GraphProjectionDeclarationSources.AllMembers + ", typeof(global::Library.ReadModels.Graphs.Person)"),
        given.GraphProjectionDeclarationSources.Marker(members: "new System.Type[] { }"),
        given.GraphProjectionDeclarationSources.Marker(members: "null"),
        given.GraphProjectionDeclarationSources.Marker(name: "not valid"),
        given.GraphProjectionDeclarationSources.Marker(name: "")
    ];
    readonly List<ScreenplayGenerationResult> _results = [];
    readonly List<(bool Stable, int Graphs, int Projections, bool Bound)> _documents = [];

    void Because()
    {
        foreach (var marker in InvalidMarkers)
        {
            Generate(given.GraphProjectionDeclarationSources.Files(marker));
            _documents.Add((RoundTrip.IsStable, Bound.Value.Model.Application.Graphs.Length,
                Bound.Value.Model.Application.GraphProjections.Length, Bound.Success));
            _results.Add(Result);
        }
    }

    [Fact] void should_keep_all_documents_stable_and_bound() => _documents.TrueForAll(document => document.Stable && document.Bound).ShouldBeTrue();
    [Fact] void should_keep_every_shape() => _documents.TrueForAll(document => document.Graphs == 1).ShouldBeTrue();
    [Fact] void should_withhold_every_invalid_projection_set() => _documents.TrueForAll(document => document.Projections == 0).ShouldBeTrue();
    [Fact] void should_diagnose_every_invalid_set() => _results.TrueForAll(result => result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.InvalidGraphProjectionDeclaration)).ShouldBeTrue();
    [Fact] void should_not_replace_invalid_metadata_with_fallback() => _results.TrueForAll(result => result.Diagnostics.All(diagnostic => diagnostic.Code != ScreenplayDiagnosticCodes.CanonicalGraphPlacement)).ShouldBeTrue();
    [Fact] void should_report_the_graph_and_marker_or_missing_member() => _results.TrueForAll(result => result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.InvalidGraphProjectionDeclaration && diagnostic.Message.Contains("People", StringComparison.Ordinal) && diagnostic.Message.Contains("member", StringComparison.Ordinal))).ShouldBeTrue();
}
