// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Artifacts sitting in the root namespace leave the module, the feature and the slice with nothing to be named
/// after but the assembly, so all of them end up saying the same word. Naming them anything else would be fiction -
/// the source really does say nothing about where they belong - so the document is left honest and what would fix
/// it is reported instead.
/// </summary>
public class artifacts_in_the_root_namespace : Specification
{
    const string Source = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library;

        [EventType]
        public record StuffDone(string What);

        [Command]
        public record DoStuff(string What)
        {
            public StuffDone Handle() => new(What);
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_still_recover_the_slice() => _analysis.Slice().Namespace.ShouldEqual("Library");
    [Fact] void should_still_recover_what_it_declares() => _analysis.Slice().Commands.Single().Name.ShouldEqual("DoStuff");
    [Fact] void should_report_that_there_is_nothing_to_arrange_by() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.NamespaceWithoutStructure);
    [Fact] void should_say_what_would_fix_it() => _analysis.Diagnostics.First(_ => _.Code == ScreenplayDiagnosticCodes.NamespaceWithoutStructure).Message.Contains("namespace of its own", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_it_as_information_since_the_document_is_still_valid() => _analysis.Diagnostics.First(_ => _.Code == ScreenplayDiagnosticCodes.NamespaceWithoutStructure).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
}
