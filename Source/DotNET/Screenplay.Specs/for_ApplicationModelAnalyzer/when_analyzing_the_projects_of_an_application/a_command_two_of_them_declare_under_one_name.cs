// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing_the_projects_of_an_application;

/// <summary>
/// Two projects declaring into one namespace write one slice, and everything within a slice is named once - a
/// document declaring two commands called <c>PlaceOrder</c> in one slice says the same word twice and means it
/// differently. Only the first can be described, so the second has to be reported rather than dropped where nobody
/// would see it.
/// </summary>
public class a_command_two_of_them_declare_under_one_name : Specification
{
    const string First = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Ordering.Placing;

        [EventType]
        public record OrderPlaced(string Reference);

        [Command]
        public record PlaceOrder(string Reference)
        {
            public OrderPlaced Handle() => new(Reference);
        }
        """;

    const string Second = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Ordering.Placing;

        [EventType]
        public record OrderRejected(string Reference);

        [Command]
        public record PlaceOrder(string Reference)
        {
            public OrderRejected Handle() => new(Reference);
        }
        """;

    Compilation _adapter;
    Compilation _application;
    ApplicationModelAnalysis _analysis;
    SliceModel _slice;
    ScreenplayDiagnostic _reported;

    void Establish()
    {
        _adapter = Analyzed.Project(
            "Library.Adapter",
            [],
            ("Source/Library.Adapter/Adapter.cs", "namespace Library.Adapter;"),
            ("Source/Library.Adapter/Ordering/Placing/Placing.cs", Second));

        _application = Analyzed.Project(
            "Library",
            [],
            ("Source/Library/Program.cs", "namespace Library;"),
            ("Source/Library/Ordering/Placing/Placing.cs", First));
    }

    void Because()
    {
        _analysis = Analyzed.Projects(_adapter, _application);
        _slice = _analysis.Model.Slices.Single();
        _reported = _analysis.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.RepeatedDeclarationAcrossProjects);
    }

    [Fact] void should_compile_the_one_project() => Analyzed.ErrorsIn(_adapter).ShouldBeEmpty();
    [Fact] void should_compile_the_other_project() => Analyzed.ErrorsIn(_application).ShouldBeEmpty();
    [Fact] void should_declare_the_command_once() => _slice.Commands.Count().ShouldEqual(1);
    [Fact] void should_keep_the_one_the_first_project_read_declares() => _slice.Commands.Single().Produces.Single().EventName.ShouldEqual("OrderPlaced");
    [Fact] void should_keep_everything_that_does_not_repeat_a_name() => _slice.Events.Select(_ => _.Name).ShouldContainOnly(["OrderPlaced", "OrderRejected"]);
    [Fact] void should_say_what_was_left_out() => _reported.Message.Contains("'PlaceOrder' is a command", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_locate_the_report_at_the_slice() => _reported.Location.ShouldEqual("Library.Ordering.Placing");
    [Fact] void should_report_it_as_a_loss() => _reported.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
}
