// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing_the_projects_of_an_application;

/// <summary>
/// Projects checked out beside each other in unrelated places share nothing but the root of the file system, and
/// writing a path relative to that is not writing it relative to anything - it leaves the machine's own layout
/// behind while looking like a relative path, which is the one thing a path in a document must never do. Each
/// project's own root is used instead, and a path then says where a file sits within its project and nothing about
/// which project that is, so it is said outright.
/// </summary>
public class projects_that_share_no_directory : Specification
{
    const string Placing = """
        using Cratis.Chronicle.Events;

        namespace Library.Ordering.Placing;

        [EventType]
        public record OrderPlaced(string Reference);
        """;

    const string Shipping = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Reactors;
        using Library.Ordering.Placing;

        namespace Library.Shipping.Dispatching;

        public class Dispatcher : IReactor
        {
            public Task Dispatch(OrderPlaced @event, EventContext context) => Task.CompletedTask;
        }
        """;

    Compilation _contracts;
    Compilation _application;
    ApplicationModelAnalysis _analysis;
    ScreenplayDiagnostic _reported;

    void Establish()
    {
        _contracts = Analyzed.Project(
            "Library.Contracts",
            [],
            ("/partners/contracts/Contracts.cs", "namespace Library.Contracts;"),
            ("/partners/contracts/Ordering/Placing/Placing.cs", Placing));

        _application = Analyzed.Project(
            "Library",
            [_contracts.ToMetadataReference()],
            ("/library/source/Program.cs", "namespace Library;"),
            ("/library/source/Shipping/Dispatching/Dispatching.cs", Shipping));
    }

    void Because()
    {
        _analysis = Analyzed.Projects(_contracts, _application);
        _reported = _analysis.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.ProjectsWithoutASharedRoot);
    }

    ReactorModel Reactor => _analysis.Model.Slices.SelectMany(_ => _.Reactors).Single();

    [Fact] void should_compile_the_contracts_project() => Analyzed.ErrorsIn(_contracts).ShouldBeEmpty();
    [Fact] void should_compile_the_application_project() => Analyzed.ErrorsIn(_application).ShouldBeEmpty();
    [Fact] void should_write_the_path_relative_to_the_root_of_its_own_project() => Reactor.SourceFilePath.ShouldEqual("Shipping/Dispatching/Dispatching.cs");
    [Fact] void should_leave_the_layout_of_the_machine_out_of_the_path() => Reactor.SourceFilePath!.StartsWith('/').ShouldBeFalse();
    [Fact] void should_say_how_many_projects_could_not_be_placed_together() => _reported.Message.Contains("The 2 projects", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_it_as_a_loss() => _reported.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
}
