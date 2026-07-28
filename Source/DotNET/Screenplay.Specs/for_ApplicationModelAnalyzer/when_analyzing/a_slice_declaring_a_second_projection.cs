// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A slice may declare at most one projection, and a document declaring two does not compile at all. Keeping the
/// first and reporting the second is what keeps the rest of the document valid, where emitting both would lose
/// everything.
/// </summary>
public class a_slice_declaring_a_second_projection : Specification
{
    const string Source = """
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

        [ReadModel]
        [FromEvent<AuthorRegistered>]
        public record AuthorSummary
        {
            [SetFrom<AuthorRegistered>("name")]
            public string Name { get; init; } = string.Empty;
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_keep_exactly_one_projection() => _analysis.Slice().Projection.ShouldNotBeNull();
    [Fact] void should_keep_the_first_one_it_read() => _analysis.Slice().Projection!.ReadModel.ShouldEqual("Author");
    [Fact] void should_report_the_one_it_left_out() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.UnmappableProjectionConstruct);
    [Fact] void should_say_a_slice_may_declare_only_one() => _analysis.Diagnostics.Any(_ => _.Message.Contains("a slice may declare at most one", StringComparison.Ordinal)).ShouldBeTrue();
}
