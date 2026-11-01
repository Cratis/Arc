// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing_the_projects_of_an_application;

public class an_aggregate_state_guard_seen_through_references_of_its_own : Specification
{
    Compilation _domain;
    Compilation _application;
    Compilation _otherDomain;
    ApplicationModelAnalysis _analysis;

    void Establish()
    {
        const string domainSource = """
                using System.Threading.Tasks;
                using Cratis.Arc.Chronicle.Aggregates;
                using Cratis.Chronicle.Events;
                namespace Library.Lending.Reservations;
                [EventType] public record BookReserved(string Isbn);
                public class ReservationState : AggregateRoot
                {
                    protected bool IsReserved { get; set; }
                }
                public class Reservation : ReservationState
                {
                    bool _closed;
                    public Task Reserve(string isbn)
                    {
                        if (IsReserved || _closed) return Task.CompletedTask;
                        return Apply(new BookReserved(isbn));
                    }
                    public void Close() => _closed = true;
                    public static bool StaticState { get; set; }
                    public bool ReadClosed() => _closed;
                    public bool ReadInherited() => IsReserved;
                    public bool ReadStatic() => StaticState;
                }
                """;
        const string applicationSource = """
                using System.Threading.Tasks;
                using Cratis.Arc.Commands.ModelBound;
                using Library.Lending.Reservations;
                namespace Library.Lending.Reserving;
                [Command] public record ReserveBook(string Isbn)
                {
                    public async Task Handle(Reservation reservation)
                    {
                        await reservation.Reserve(Isbn);
                        await reservation.Commit();
                    }
                }
                """;
        _domain = Analyzed.Project(
            "Library.Domain",
            [],
            ("Source/Library.Domain/Domain.cs", "namespace Library.Domain;"),
            ("Source/Library.Domain/Lending/Reservations/Reservations.cs", domainSource));
        _otherDomain = _domain.WithAssemblyName("Other.Library.Domain");
        _application = Analyzed.Project(
            "Library.Application",
            [],
            ("Source/Library.Application/Application.cs", "namespace Library.Application;"),
            ("Source/Library.Application/Lending/Reserving/Reserving.cs", applicationSource))
            .WithReferences(((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator).Select(_ => MetadataReference.CreateFromFile(_))
                .Append<MetadataReference>(_domain.ToMetadataReference()));
    }

    void Because() => _analysis = Analyzed.Projects(_domain, _application);

    bool ReadsState(string method, Compilation aggregateCompilation)
    {
        var source = _domain.GetTypeByMetadataName("Library.Lending.Reservations.Reservation")!;
        var declaration = (MethodDeclarationSyntax)source.GetMembers(method).Single().DeclaringSyntaxReferences.Single().GetSyntax();
        var expression = declaration.ExpressionBody!.Expression;
        return AggregateState.IsReadBy(
            expression,
            _domain.GetSemanticModel(expression.SyntaxTree),
            aggregateCompilation.GetTypeByMetadataName("Library.Lending.Reservations.Reservation"));
    }

    [Fact] void should_compile_the_other_assembly() => Analyzed.ErrorsIn(_otherDomain).ShouldBeEmpty();
    [Fact] void should_distinguish_the_other_assembly() => _otherDomain.Assembly.Identity.Equals(_domain.Assembly.Identity).ShouldBeFalse();
    [Fact] void should_recognize_local_instance_state() => ReadsState("ReadClosed", _domain).ShouldBeTrue();
    [Fact] void should_recognize_inherited_instance_state() => ReadsState("ReadInherited", _domain).ShouldBeTrue();
    [Fact] void should_recognize_retargeted_instance_state() => ReadsState("ReadClosed", _application).ShouldBeTrue();
    [Fact] void should_recognize_retargeted_inherited_state() => ReadsState("ReadInherited", _application).ShouldBeTrue();
    [Fact] void should_not_recognize_static_state() => ReadsState("ReadStatic", _application).ShouldBeFalse();
    [Fact] void should_not_recognize_state_from_another_assembly() => ReadsState("ReadClosed", _otherDomain).ShouldBeFalse();
    [Fact] void should_compile_the_domain() => Analyzed.ErrorsIn(_domain).ShouldBeEmpty();
    [Fact] void should_compile_the_application() => Analyzed.ErrorsIn(_application).ShouldBeEmpty();
    [Fact] void should_compare_distinct_declaring_types() => SymbolEqualityComparer.Default.Equals(
        _domain.GetTypeByMetadataName("Library.Lending.Reservations.Reservation"),
        _application.GetTypeByMetadataName("Library.Lending.Reservations.Reservation")).ShouldBeFalse();
    [Fact] void should_recognize_the_state_guard() => _analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.UnmappableAggregateStateCondition).ShouldBeTrue();
    [Fact] void should_not_misreport_it_as_an_unknown_guard() => _analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction).ShouldBeFalse();
}
