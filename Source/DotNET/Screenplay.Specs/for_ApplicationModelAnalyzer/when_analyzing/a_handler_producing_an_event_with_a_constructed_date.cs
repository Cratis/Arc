// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Date literals are read only where a date comparison needs them. An event mapping from a constructed date is
/// not a literal the mapping reader recognizes, so the property stays out of the production as it did before date
/// comparisons were read.
/// </summary>
public class a_handler_producing_an_event_with_a_constructed_date : Specification
{
    const string Source = """
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Lending.Reserving;

        [EventType]
        public record BookReserved(string Title, DateOnly Due);

        [Command]
        public record ReserveBook(string Title)
        {
            public object Handle() => new BookReserved(Title, new DateOnly(2026, 1, 1));
        }
        """;

    ApplicationModelAnalysis _analysis;
    ProducesModel _produced;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _produced = _analysis.Slice().Commands.First().Produces.FirstOrDefault();
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_still_produce_the_event() => _produced.ShouldNotBeNull();
    [Fact] void should_not_read_the_constructed_date_as_a_literal() => _produced.Mappings.Any(mapping => mapping.Source is LiteralSource).ShouldBeFalse();
    [Fact] void should_report_the_left_out_due_property() => _analysis.Diagnostics.ShouldContain(diagnostic =>
        diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction && diagnostic.Message.Contains("'BookReserved.Due'"));
}
