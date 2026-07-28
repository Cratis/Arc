// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A command that hands its work to an aggregate root constructs no event of its own, so reading only the handler
/// would describe a command that produces nothing. The behavior the handler calls is read as well, and the input the
/// handler passed along stands in for the parameters the aggregate root named it, so the mappings say where each
/// value really comes from rather than that it came from code.
/// </summary>
public class a_command_governed_by_an_aggregate_root : Specification
{
    const string Source = """
        using System.Threading.Tasks;
        using Cratis.Arc.Chronicle.Aggregates;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Lending.Reserving;

        [EventType]
        public record BookReserved(string Isbn, string Member, string Branch);

        public class Reservation : AggregateRoot
        {
            public Task Reserve(string isbn, string member) => Apply(new BookReserved(isbn, member, "central"));

            public void OnBookReserved(BookReserved @event)
            {
            }
        }

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

    ApplicationModelAnalysis _analysis;
    ProducesModel _produces;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _produces = _analysis.Slice().Commands.First().Produces.First();
    }

    MappingSourceModel Mapping(string property) => _produces.Mappings.First(_ => _.Property == property).Source;

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_state_what_the_command_produces() => _analysis.Slice().Commands.First().Produces.Select(_ => _.EventName).ShouldContainOnly(["BookReserved"]);
    [Fact] void should_produce_it_unconditionally() => _produces.When.ShouldBeNull();
    [Fact] void should_follow_the_first_value_back_to_the_command_input() => Mapping("Isbn").ShouldEqual(new PropertyPathSource("Isbn"));
    [Fact] void should_follow_the_second_value_back_to_the_command_input() => Mapping("Member").ShouldEqual(new PropertyPathSource("MemberId"));
    [Fact] void should_keep_a_value_the_aggregate_root_decides_on_its_own() => Mapping("Branch").ShouldEqual(new LiteralSource("central"));
    [Fact] void should_call_the_slice_a_state_change() => _analysis.Slice().Kind.ShouldEqual(SliceKind.StateChange);
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
