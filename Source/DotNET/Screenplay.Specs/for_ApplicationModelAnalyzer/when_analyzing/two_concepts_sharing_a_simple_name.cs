// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A concept is declared once at the top of the document and referred to by its simple name, so two types in two
/// namespaces sharing that name cannot both be described. Keeping the first is the only choice left - but the second
/// one is then quietly described as something it is not, which is the one outcome a reader cannot detect.
/// </summary>
public class two_concepts_sharing_a_simple_name : Specification
{
    const string InOneSlice = """
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;

        namespace Library.Authors.Registration;

        public record Identifier(string Value) : ConceptAs<string>(Value);

        [EventType]
        public record AuthorRegistered(Identifier Id);
        """;

    const string InAnother = """
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;

        namespace Library.Lending.Reserving;

        public record Identifier(int Value) : ConceptAs<int>(Value);

        [EventType]
        public record BookReserved(Identifier Id);
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Registration/Registration.cs", InOneSlice),
        ("Library/Lending/Reserving/Reserving.cs", InAnother)
    ];

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(_sources);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_declare_the_name_once() => _analysis.Model.Concepts.Count(_ => _.Name == "Identifier").ShouldEqual(1);
    [Fact] void should_report_the_one_it_could_not_describe() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.AmbiguousConceptName);
    [Fact] void should_name_the_type_it_could_not_describe() => _analysis.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.AmbiguousConceptName).Message.ShouldContain("Library.Lending.Reserving.Identifier");
}
