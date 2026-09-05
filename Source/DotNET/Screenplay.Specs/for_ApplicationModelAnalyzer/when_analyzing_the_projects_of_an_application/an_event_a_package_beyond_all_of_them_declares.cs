// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing_the_projects_of_an_application;

/// <summary>
/// A sibling project stops being something to import because it is part of the application. A package is not, and
/// leaving the projects out of the search must not leave the packages out with them - an event a sibling bounded
/// context publishes is still real, still referred to, and still has to be stated rather than left as a name the
/// document never introduces.
/// </summary>
public class an_event_a_package_beyond_all_of_them_declares : Specification
{
    const string Package = """
        using Cratis.Chronicle.Events;

        namespace Partners.Contracts;

        [EventType]
        public record InvitationAccepted(string Email);
        """;

    const string Contracts = """
        using Cratis.Chronicle.Events;

        namespace Library.Ordering.Placing;

        [EventType]
        public record OrderPlaced(string Reference);
        """;

    const string Slice = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Reactors;
        using Partners.Contracts;

        namespace Library.Admin.Invitations;

        public class AcceptedInvitationProvisioner : IReactor
        {
            public Task Provision(InvitationAccepted @event, EventContext context) => Task.CompletedTask;
        }
        """;

    MetadataReference _package;
    Compilation _contracts;
    Compilation _application;
    ApplicationModelAnalysis _analysis;

    void Establish()
    {
        _package = Analyzed.Package("Partners.Contracts", Package);

        _contracts = Analyzed.Project(
            "Library.Contracts",
            [],
            ("Source/Library.Contracts/Contracts.cs", "namespace Library.Contracts;"),
            ("Source/Library.Contracts/Ordering/Placing/Placing.cs", Contracts));

        _application = Analyzed.Project(
            "Library",
            [_contracts.ToMetadataReference(), _package],
            ("Source/Library/Program.cs", "namespace Library;"),
            ("Source/Library/Admin/Invitations/Provision.cs", Slice));
    }

    void Because() => _analysis = Analyzed.Projects(_application, _contracts);

    [Fact] void should_compile_the_contracts_project() => Analyzed.ErrorsIn(_contracts).ShouldBeEmpty();
    [Fact] void should_compile_the_application_project() => Analyzed.ErrorsIn(_application).ShouldBeEmpty();
    [Fact] void should_import_the_event_the_package_declares() => _analysis.Model.Imports.ShouldContainOnly(["Partners.Contracts.InvitationAccepted"]);
    [Fact] void should_not_import_the_event_a_project_of_the_application_declares() => _analysis.Model.Imports.Any(_ => _.Contains("OrderPlaced", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_report_anything_as_undeclared() => _analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.EventDeclaredOutsideCompilation).ShouldBeFalse();
}
