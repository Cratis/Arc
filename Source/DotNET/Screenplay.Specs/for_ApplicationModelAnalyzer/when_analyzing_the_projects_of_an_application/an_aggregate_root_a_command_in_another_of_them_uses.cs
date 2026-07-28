// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing_the_projects_of_an_application;

/// <summary>
/// What an aggregate root applies is stated through the command that hands it its work, and an aggregate root
/// nothing calls is reported because its events then go unstated. An aggregate root in a domain project called by a
/// command in the project above it is called by the application, and reading the two projects apart would report
/// every aggregate root of a layered application as one nothing uses.
/// </summary>
public class an_aggregate_root_a_command_in_another_of_them_uses : Specification
{
    const string Domain = """
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

    const string Application = """
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

    Compilation _domain;
    Compilation _application;
    ApplicationModelAnalysis _analysis;

    void Establish()
    {
        _domain = Analyzed.Project(
            "Library.Domain",
            [],
            ("Source/Library.Domain/Domain.cs", "namespace Library.Domain;"),
            ("Source/Library.Domain/Lending/Reservations/Reservations.cs", Domain));

        _application = Analyzed.Project(
            "Library.Application",
            [_domain.ToMetadataReference()],
            ("Source/Library.Application/Application.cs", "namespace Library.Application;"),
            ("Source/Library.Application/Lending/Reserving/Reserving.cs", Application));
    }

    void Because() => _analysis = Analyzed.Projects(_domain, _application);

    ProducesModel Produces => _analysis.Model.Slices.SelectMany(_ => _.Commands).Single().Produces.Single();

    [Fact] void should_compile_the_domain_project() => Analyzed.ErrorsIn(_domain).ShouldBeEmpty();
    [Fact] void should_compile_the_application_project() => Analyzed.ErrorsIn(_application).ShouldBeEmpty();
    [Fact] void should_state_the_event_the_aggregate_root_applies_as_produced_by_the_command() => Produces.EventName.ShouldEqual("BookReserved");
    [Fact] void should_not_report_the_aggregate_root_as_one_nothing_uses() => _analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.AggregateRootWithoutCounterpart).ShouldBeFalse();
}
