// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Source that builds and declares no artifact is a different thing from source that does not build, and conflating
/// the two would tell a reader their application is empty when really it is broken. Only the first is worth stating
/// as "declares nothing", and neither is an error - an application with no Arc artifacts yet is a perfectly ordinary
/// thing to point the generator at.
/// </summary>
public class source_that_compiles_but_declares_nothing : Specification
{
    const string Source = """
        namespace Library.Plumbing;

        public class NotAnArtifact
        {
            public string Name { get; set; } = string.Empty;
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    [Fact] void should_be_analyzing_source_that_really_does_compile() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_report_that_it_declares_nothing() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.AnalysisUnavailable]);
    [Fact] void should_not_claim_the_source_failed_to_compile() => _analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.SourceDidNotCompile).ShouldBeFalse();
    [Fact] void should_report_it_as_information_only() => _analysis.Diagnostics.Single().Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
    [Fact] void should_recover_no_slice() => _analysis.Model.Slices.ShouldBeEmpty();
}
