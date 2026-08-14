// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A slice declares as many projections as its behavior needs. The read model a screen binds to and the one a command
/// reads to decide are two projections of one behavior, and an application routinely writes both - so keeping
/// whichever happened to be catalogued first stated one of them and silently dropped the rest of the read side.
/// </summary>
public class a_slice_declaring_several_projections : Specification
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
    [Fact] void should_keep_both_of_them() => _analysis.Slice().Projections.Select(_ => _.ReadModel).ShouldContainOnly(["Author", "AuthorSummary"]);
    [Fact] void should_leave_neither_out() => _analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.UnmappableProjectionConstruct).ShouldBeFalse();
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
