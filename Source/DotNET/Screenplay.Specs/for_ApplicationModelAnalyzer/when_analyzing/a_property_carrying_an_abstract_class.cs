// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// An abstract class leaves the same gap an interface does and for the same reason - what a value of it holds is
/// settled by whatever derives from it - so it is reported the same way. It is worth saying separately because it is
/// the shape that reaches real documents: an application describing a screen carries a base element far more often
/// than it carries a bare interface.
/// </summary>
public class a_property_carrying_an_abstract_class : Specification
{
    const string Source = """
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        public abstract class UIElement
        {
            public string Kind { get; init; } = string.Empty;
        }

        [EventType]
        public record AuthorRegistered(string Name, UIElement Element);
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_report_a_name_the_document_never_declares() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.UnmappableTypeReference);
    [Fact] void should_name_the_contract_it_could_not_declare() => _analysis.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.UnmappableTypeReference).Message.ShouldContain("UIElement");
    [Fact] void should_say_nothing_about_the_property_it_could_express() => _analysis.Diagnostics.Count(_ => _.Code == ScreenplayDiagnosticCodes.UnmappableTypeReference).ShouldEqual(1);
}
