// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// An interface survives being written as the single identifier the grammar allows, so nothing is lost in the writing.
/// What cannot be written is the declaration: a <c>type</c> says what a value holds, and what an implementation holds
/// is exactly what a contract leaves open. The property names something the document never introduces, which is the
/// same dangling reference a constructed generic leaves, so it is said rather than passed off as an answer.
/// </summary>
public class a_property_carrying_an_interface : Specification
{
    const string Source = """
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        public interface IRule;

        [EventType]
        public record AuthorRegistered(string Name, IRule Rule);
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_report_a_name_the_document_never_declares() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.UnmappableTypeReference);
    [Fact] void should_name_the_contract_it_could_not_declare() => _analysis.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.UnmappableTypeReference).Message.ShouldContain("IRule");
    [Fact] void should_say_nothing_about_the_property_it_could_express() => _analysis.Diagnostics.Count(_ => _.Code == ScreenplayDiagnosticCodes.UnmappableTypeReference).ShouldEqual(1);
}
