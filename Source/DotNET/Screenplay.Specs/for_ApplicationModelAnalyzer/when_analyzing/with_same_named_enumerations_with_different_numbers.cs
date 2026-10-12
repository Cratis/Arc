// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

public class with_same_named_enumerations_with_different_numbers : Specification
{
    const string Source = """
        using Cratis.Chronicle.Events;
        namespace Library.First { public enum Status { Draft = 1, Issued = 4 } [EventType] public record FirstPlaced(Status Status); }
        namespace Library.Second { public enum Status { Draft = 0, Issued = 1 } [EventType] public record SecondPlaced(Status Status); }
        """;

    ApplicationModelAnalysis _result;

    void Because() => _result = Analyzed.Source(Source);

    [Fact] void should_compile_the_source() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_report_the_conflicting_meaning() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.AmbiguousConceptName).Message.ShouldContain("Library.Second.Status");
    [Fact] void should_keep_the_first_numbers() => _result.Model.Concepts.Single().EnumNumbers.Select(number => (int)number).ShouldEqual([1, 4]);
}
