// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing_the_projects_of_an_application;

/// <summary>
/// An event a referenced package declares is imported, because it is real and outside the application. A sibling
/// project reaches the analyzer through exactly the same door - a project reference is a referenced assembly - and
/// it is not outside the application at all. Importing it would state a dependency on part of the application
/// itself, so it has to be declared like anything else the application holds.
/// </summary>
public class an_event_a_sibling_project_declares : Specification
{
    const string Contracts = """
        using Cratis.Chronicle.Events;

        namespace Partners.Invitations;

        [EventType]
        public record InvitationAccepted(string Email);
        """;

    const string Slice = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Reactors;
        using Partners.Invitations;

        namespace Library.Admin.Invitations;

        public class AcceptedInvitationProvisioner : IReactor
        {
            public Task Provision(InvitationAccepted @event, EventContext context) => Task.CompletedTask;
        }
        """;

    Compilation _contracts;
    Compilation _application;
    ApplicationModelAnalysis _analysis;

    void Establish()
    {
        _contracts = Analyzed.Project(
            "Partners.Contracts",
            [],
            ("Source/Partners.Contracts/Contracts.cs", "namespace Partners;"),
            ("Source/Partners.Contracts/Invitations/Invitations.cs", Contracts));

        _application = Analyzed.Project(
            "Library",
            [_contracts.ToMetadataReference()],
            ("Source/Library/Program.cs", "namespace Library;"),
            ("Source/Library/Admin/Invitations/Provision.cs", Slice));
    }

    void Because() => _analysis = Analyzed.Projects(_application, _contracts);

    SliceModel SliceIn(string @namespace) => _analysis.Model.Slices.Single(_ => string.Equals(_.Namespace, @namespace, StringComparison.Ordinal));

    [Fact] void should_compile_the_contracts_project() => Analyzed.ErrorsIn(_contracts).ShouldBeEmpty();
    [Fact] void should_compile_the_application_project() => Analyzed.ErrorsIn(_application).ShouldBeEmpty();
    [Fact] void should_declare_the_event_in_the_slice_the_sibling_project_wrote_it_in() => SliceIn("Partners.Invitations").Events.Single().Name.ShouldEqual("InvitationAccepted");
    [Fact] void should_still_observe_it_from_the_reactor() => SliceIn("Library.Admin.Invitations").Reactors.Single().ObservedEvents.ShouldContainOnly(["InvitationAccepted"]);
    [Fact] void should_import_nothing() => _analysis.Model.Imports.ShouldBeEmpty();
    [Fact] void should_not_report_it_as_undeclared() => _analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.EventDeclaredOutsideCompilation).ShouldBeFalse();
    [Fact] void should_report_nothing_at_all() => _analysis.Diagnostics.ShouldBeEmpty();
}
