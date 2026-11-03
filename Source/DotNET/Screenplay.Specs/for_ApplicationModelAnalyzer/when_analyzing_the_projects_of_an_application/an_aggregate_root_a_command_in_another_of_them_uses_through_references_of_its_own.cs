// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing_the_projects_of_an_application;

/// <summary>
/// Each project of a real application carries references of its own, and when the project above the domain binds them
/// to other assembly symbols than the domain did, the compiler hands it a retargeted copy of every domain type rather
/// than the type itself. An aggregate root reached through such a copy is still the aggregate root the domain
/// declares, and telling the two apart by symbol would report it as one nothing uses.
/// </summary>
public class an_aggregate_root_a_command_in_another_of_them_uses_through_references_of_its_own : Specification
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
    MetadataReference _domainReference;
    Compilation _application;
    ApplicationModelAnalysis _analysis;

    void Establish()
    {
        _domain = Analyzed.Project(
            "Library.Domain",
            [],
            ("Source/Library.Domain/Domain.cs", "namespace Library.Domain;"),
            ("Source/Library.Domain/Lending/Reservations/Reservations.cs", Domain));
        _domainReference = _domain.ToMetadataReference();

        _application = Analyzed.Project(
            "Library.Application",
            [],
            ("Source/Library.Application/Application.cs", "namespace Library.Application;"),
            ("Source/Library.Application/Lending/Reserving/Reserving.cs", Application))
            .WithReferences(PlatformReferencesOfItsOwn().Append(_domainReference));
    }

    void Because() => _analysis = Analyzed.Projects(_domain, _application);

    [Fact] void should_compile_the_domain_project() => Analyzed.ErrorsIn(_domain).ShouldBeEmpty();
    [Fact] void should_compile_the_application_project() => Analyzed.ErrorsIn(_application).ShouldBeEmpty();
    [Fact] void should_see_the_domain_project_through_a_copy_of_it() => SymbolEqualityComparer.Default.Equals(_application.GetAssemblyOrModuleSymbol(_domainReference), _domain.Assembly).ShouldBeFalse();
    [Fact] void should_state_the_event_the_aggregate_root_applies_as_produced_by_the_command() => _analysis.Model.Slices.SelectMany(_ => _.Commands).Single().Produces.Single().EventName.ShouldEqual("BookReserved");
    [Fact] void should_compare_distinct_behavior_parameters() => SymbolEqualityComparer.Default.Equals(
        _domain.GetTypeByMetadataName("Library.Lending.Reservations.Reservation")!.GetMembers("Reserve").OfType<IMethodSymbol>().Single().Parameters[0],
        _application.GetTypeByMetadataName("Library.Lending.Reservations.Reservation")!.GetMembers("Reserve").OfType<IMethodSymbol>().Single().Parameters[0]).ShouldBeFalse();
    [Fact] void should_follow_the_first_argument() => _analysis.Model.Slices.SelectMany(_ => _.Commands).Single().Produces.Single().Mappings.Single(_ => _.Property == "Isbn").Source.ShouldEqual(new PropertyPathSource("Isbn"));
    [Fact] void should_follow_the_second_argument() => _analysis.Model.Slices.SelectMany(_ => _.Commands).Single().Produces.Single().Mappings.Single(_ => _.Property == "Member").Source.ShouldEqual(new PropertyPathSource("MemberId"));
    [Fact] void should_not_report_the_aggregate_root_as_one_nothing_uses() => _analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.AggregateRootWithoutCounterpart).ShouldBeFalse();

    static IEnumerable<MetadataReference> PlatformReferencesOfItsOwn() =>
        ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(_ => MetadataReference.CreateFromFile(_));
}
