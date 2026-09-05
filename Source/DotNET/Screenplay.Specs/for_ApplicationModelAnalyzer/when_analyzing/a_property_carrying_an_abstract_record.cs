// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Being abstract is not on its own what leaves a name undeclared. A record is declared whether or not it is abstract,
/// so a property carrying one names a shape the document does introduce, and reporting it would say a reference is
/// dangling that resolves. This is the line between the two - what is reported is a type no declaration is written
/// for, not a type something can derive from.
/// </summary>
public class a_property_carrying_an_abstract_record : Specification
{
    const string Source = """
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        public abstract record Outline(string Kind);

        [EventType]
        public record AuthorRegistered(string Name, Outline Shape);
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_declare_the_shape_the_property_names() => _analysis.Model.Types.Any(_ => _.Name == "Outline").ShouldBeTrue();
    [Fact] void should_report_nothing_the_document_declares() => _analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.UnmappableTypeReference).ShouldBeFalse();
}
