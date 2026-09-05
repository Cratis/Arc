// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A handler that decides between two events is the whole reason a produces block can carry a condition. Reading the
/// branch the construction sits in recovers the decision, and what follows a guard clause that returns is reached
/// only when the guard did not hold - stating it unconditionally would describe a different application.
/// </summary>
public class a_handler_deciding_between_events : Specification
{
    const string Source = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Lending.Reserving;

        [EventType]
        public record BookReserved(string Isbn);

        [EventType]
        public record PremiumReservationGranted(string Isbn);

        [Command]
        public record ReserveBook(string Isbn, int Tier)
        {
            public object Handle()
            {
                if (Tier > 1)
                {
                    return new PremiumReservationGranted(Isbn);
                }

                return new BookReserved(Isbn);
            }
        }
        """;

    ApplicationModelAnalysis _analysis;
    IEnumerable<ProducesModel> _produces;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _produces = _analysis.Slice().Commands.First().Produces;
    }

    ProducesModel Produced(string name) => _produces.First(_ => _.EventName == name);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_produce_both_events() => _produces.Select(_ => _.EventName).ShouldContainOnly(["PremiumReservationGranted", "BookReserved"]);
    [Fact] void should_guard_the_branch_it_was_written_in() => Produced("PremiumReservationGranted").When.ShouldEqual(new ComparisonCondition("Tier", ComparisonKind.GreaterThan, new LiteralSource(1)));
    [Fact] void should_guard_the_fall_through_with_the_opposite() => Produced("BookReserved").When.ShouldEqual(new ComparisonCondition("Tier", ComparisonKind.LessThanOrEqual, new LiteralSource(1)));
    [Fact] void should_map_from_the_command_input() => Produced("BookReserved").Mappings.Single().Source.ShouldEqual(new PropertyPathSource("Isbn"));
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
