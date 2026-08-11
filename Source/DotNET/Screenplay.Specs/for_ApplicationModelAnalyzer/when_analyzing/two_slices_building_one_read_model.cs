// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A read model is what a projection builds, and Screenplay holds one builder for each - a document naming two of
/// them does not compile at all, whichever slices they are written in. So the rule is document wide rather than slice
/// wide, and the second builder is turned away and reported rather than emitted into a document nothing can read.
/// </summary>
/// <remarks>
/// This is the half of the old "a slice declares at most one projection" rule that was real. Dropping every
/// projection after the first was too broad - it cost read models that were never in conflict - but dropping none at
/// all writes a document that does not compile, which loses the whole of it rather than one projection.
/// </remarks>
public class two_slices_building_one_read_model : Specification
{
    const string Listing = """
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;

        namespace Library.Authors.Listing;

        [EventType]
        public record AuthorRegistered(string Name);

        [ReadModel]
        [FromEvent<AuthorRegistered>]
        public record Author
        {
            [SetFrom<AuthorRegistered>("name")]
            public string Name { get; init; } = string.Empty;
        }
        """;

    const string Reporting = """
        using Cratis.Chronicle.Projections;
        using Library.Authors.Listing;

        namespace Library.Authors.Reporting;

        public class AuthorAgain : IProjectionFor<Author>
        {
            public void Define(IProjectionBuilderFor<Author> builder) => builder
                .From<AuthorRegistered>(_ => _.Set(m => m.Name).To(e => e.Name));
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Listing/Listing.cs", Listing),
        ("Library/Authors/Reporting/Reporting.cs", Reporting)
    ];

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(_sources);

    IEnumerable<string> BuildersOf(string readModel) =>
        _analysis.Model.Slices.SelectMany(_ => _.Projections).Where(_ => _.ReadModel == readModel).Select(_ => _.Identifier);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_keep_one_builder_for_the_read_model() => BuildersOf("Author").Count().ShouldEqual(1);
    [Fact] void should_report_the_one_it_left_out() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.UnmappableProjectionConstruct);
    [Fact] void should_say_the_read_model_is_built_once() => _analysis.Diagnostics.Any(_ => _.Message.Contains("a read model is built once", StringComparison.Ordinal)).ShouldBeTrue();
}
