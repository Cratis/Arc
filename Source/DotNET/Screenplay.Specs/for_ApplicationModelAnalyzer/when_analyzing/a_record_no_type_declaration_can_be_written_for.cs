// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Types;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A record an artifact carries is declared as a type, which leaves the two records no declaration can be written
/// for: one carrying no value at all, whose body would be empty, and one whose simple name something else is already
/// declared under, whose declaration would say the same word twice. Both leave a property naming a shape the document
/// never introduces, and which of the two it was is worth saying because only one of them is closed by renaming
/// something.
/// </summary>
public class a_record_no_type_declaration_can_be_written_for : Specification
{
    const string Source = """
        using System;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;

        namespace Library.Authors.Registration;

        public record AuthorId(Guid Value) : ConceptAs<Guid>(Value);

        public record Nothing();

        public record Shelf(AuthorId Owner);

        [EventType]
        public record AuthorRegistered(AuthorId Id, Nothing Empty, Shelf Where, Elsewhere.Shelf Other);
        """;

    const string Elsewhere = """
        using Cratis.Concepts;

        namespace Library.Authors.Registration.Elsewhere;

        public record Shelf(BookTitle Title);

        public record BookTitle(string Value) : ConceptAs<string>(Value);
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Registration/Registration.cs", Source),
        ("Library/Authors/Registration/Elsewhere.cs", Elsewhere)
    ];

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(_sources);

    IEnumerable<ScreenplayDiagnostic> Shapes =>
        _analysis.Diagnostics.Where(_ => _.Code == ScreenplayDiagnosticCodes.UndeclarableShape);

    ScreenplayDiagnostic Shape(string name) => Shapes.First(_ => _.Message.Contains(name, StringComparison.Ordinal));

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_declare_the_shape_it_could_write_one_for() => _analysis.Model.Types.Select(_ => _.Name).ShouldContainOnly(["Shelf"]);
    [Fact] void should_declare_the_first_of_two_records_sharing_a_name() => _analysis.Model.Types.Single().Properties.Select(_ => _.Name).ShouldContainOnly(["Owner"]);
    [Fact] void should_report_every_shape_it_could_not_declare() => Shapes.Count().ShouldEqual(2);
    [Fact] void should_say_a_record_carrying_nothing_has_nothing_to_declare() => Shape("Nothing").Message.Contains(ShapeRegistry.CarriesNothing, StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_record_that_took_the_name_first() => Shape("Elsewhere.Shelf").Message.Contains("'Library.Authors.Registration.Shelf' is already declared as 'Shelf'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_it_as_a_warning() => Shapes.All(_ => _.Severity == ScreenplayDiagnosticSeverity.Warning).ShouldBeTrue();
}
