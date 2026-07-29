// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing_the_projects_of_an_application;

/// <summary>
/// A slice is recovered from a namespace, and nothing says a namespace belongs to one project. A contracts project
/// publishing the events of a slice while the project beside it handles the command producing them writes one slice
/// from two compilations - and a document holding it twice would say the slice twice and describe half of it in
/// each.
/// </summary>
public class a_namespace_two_of_them_declare_into : Specification
{
    const string Contracts = """
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;

        namespace Library.Ordering.Placing;

        public record OrderReference(string Value) : ConceptAs<string>(Value);

        [EventType]
        public record OrderPlaced(OrderReference Reference);
        """;

    const string Handling = """
        using Cratis.Arc.Commands.ModelBound;

        namespace Library.Ordering.Placing;

        [Command]
        public record PlaceOrder(OrderReference Reference)
        {
            public OrderPlaced Handle() => new(Reference);
        }
        """;

    Compilation _contracts;
    Compilation _application;
    ApplicationModelAnalysis _analysis;
    SliceModel _slice;

    void Establish()
    {
        _contracts = Analyzed.Project(
            "Library.Contracts",
            [],
            ("Source/Library.Contracts/Contracts.cs", "namespace Library.Contracts;"),
            ("Source/Library.Contracts/Ordering/Placing/Placing.cs", Contracts));

        _application = Analyzed.Project(
            "Library",
            [_contracts.ToMetadataReference()],
            ("Source/Library/Program.cs", "namespace Library;"),
            ("Source/Library/Ordering/Placing/Placing.cs", Handling));
    }

    void Because()
    {
        _analysis = Analyzed.Projects(_contracts, _application);
        _slice = _analysis.Model.Slices.Single();
    }

    [Fact] void should_compile_the_contracts_project() => Analyzed.ErrorsIn(_contracts).ShouldBeEmpty();
    [Fact] void should_compile_the_application_project() => Analyzed.ErrorsIn(_application).ShouldBeEmpty();
    [Fact] void should_describe_the_namespace_as_one_slice() => _slice.Namespace.ShouldEqual("Library.Ordering.Placing");
    [Fact] void should_hold_the_command_the_one_project_declares() => _slice.Commands.Single().Name.ShouldEqual("PlaceOrder");
    [Fact] void should_hold_the_event_the_other_project_declares() => _slice.Events.Single().Name.ShouldEqual("OrderPlaced");
    [Fact] void should_state_what_the_command_produces() => _slice.Commands.Single().Produces.Single().EventName.ShouldEqual("OrderPlaced");
    [Fact] void should_take_the_kind_from_everything_the_projects_hold_together() => _slice.Kind.ShouldEqual(SliceKind.StateChange);
    [Fact] void should_report_nothing_at_all() => _analysis.Diagnostics.ShouldBeEmpty();
}
