// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing_the_projects_of_an_application;

/// <summary>
/// Every path in a document is written relative to a directory, or the document carries the absolute layout of the
/// machine that generated it and nobody can commit or diff it. One project answers that with its own root. Several
/// projects each have their own, and using them would leave <c>Ordering/Placing.cs</c> as the path of a file in two
/// projects with nothing saying which - so the directory they are all written under is used and every path opens
/// with the project it belongs to.
/// </summary>
public class projects_written_under_one_directory : Specification
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

    void Establish()
    {
        _contracts = Analyzed.Project(
            "Library.Contracts",
            [],
            ("Source/Library.Contracts/Contracts.cs", "namespace Library.Contracts;"),
            ("Source/Library.Contracts/Ordering/Placing/Placing.cs", Placing));

        _application = Analyzed.Project(
            "Library",
            [_contracts.ToMetadataReference()],
            ("Source/Library/Program.cs", "namespace Library;"),
            ("Source/Library/Shipping/Dispatching/Dispatching.cs", Shipping));
    }

    void Because() => _analysis = Analyzed.Projects(_contracts, _application);

    ReactorModel Reactor => _analysis.Model.Slices.SelectMany(_ => _.Reactors).Single();

    [Fact] void should_compile_the_contracts_project() => Analyzed.ErrorsIn(_contracts).ShouldBeEmpty();
    [Fact] void should_compile_the_application_project() => Analyzed.ErrorsIn(_application).ShouldBeEmpty();
    [Fact] void should_write_the_path_relative_to_the_directory_the_projects_share() => Reactor.SourceFilePath.ShouldEqual("Library/Shipping/Dispatching/Dispatching.cs");
    [Fact] void should_not_report_the_projects_as_sharing_nothing() => _analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.ProjectsWithoutASharedRoot).ShouldBeFalse();
    [Fact] void should_report_nothing_at_all() => _analysis.Diagnostics.ShouldBeEmpty();
}
