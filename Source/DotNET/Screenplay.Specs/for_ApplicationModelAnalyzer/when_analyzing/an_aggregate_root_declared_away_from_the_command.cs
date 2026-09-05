// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// An aggregate root does not have to live beside the command that uses it. The behavior is then read from one file
/// while the values it was given were written in another, so the model that can make sense of each expression has to
/// travel with it - reading both through the wrong one would lose every mapping.
/// </summary>
public class an_aggregate_root_declared_away_from_the_command : Specification
{
    const string Aggregate = """
        using System.Threading.Tasks;
        using Cratis.Arc.Chronicle.Aggregates;
        using Cratis.Chronicle.Events;

        namespace Library.Lending.Reservations;

        [EventType]
        public record BookReserved(string Isbn, string Member);

        public class Reservation : AggregateRoot
        {
            public Task Reserve(string isbn, string member) => Apply(new BookReserved(isbn, member));
        }
        """;

    const string Command = """
        using System.Threading.Tasks;
        using Cratis.Arc.Commands.ModelBound;
        using Library.Lending.Reservations;

        namespace Library.Lending.Reserving;

        [Command]
        public record ReserveBook(string Isbn, string MemberId)
        {
            public async Task Handle(Reservation reservation)
            {
                await reservation.Reserve(Isbn, MemberId);
                await reservation.Commit();
            }
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Lending/Reservations/Reservations.cs", Aggregate),
        ("Library/Lending/Reserving/Reserving.cs", Command)
    ];

    ApplicationModelAnalysis _analysis;
    ProducesModel _produces;

    void Establish()
    {
        _analysis = Analyzed.Source(_sources);
        _produces = _analysis.Model.Slices.SelectMany(_ => _.Commands).Single().Produces.Single();
    }

    MappingSourceModel Mapping(string property) => _produces.Mappings.First(_ => _.Property == property).Source;

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_state_what_the_command_produces() => _produces.EventName.ShouldEqual("BookReserved");
    [Fact] void should_follow_the_first_value_back_across_the_file_boundary() => Mapping("Isbn").ShouldEqual(new PropertyPathSource("Isbn"));
    [Fact] void should_follow_the_second_value_back_across_the_file_boundary() => Mapping("Member").ShouldEqual(new PropertyPathSource("MemberId"));
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
