// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A reactor observing what a sibling bounded context publishes refers to an event a package declares. The event is
/// real, so a document saying nothing about it refers to a name it never introduces - and Screenplay has the
/// construct for exactly this, which is what an import is for.
/// </summary>
public class an_event_a_referenced_package_declares : Specification
{
    const string Contracts = """
        using Cratis.Chronicle.Events;

        namespace Partners.Contracts;

        [EventType]
        public record InvitationToJoinAdaAccepted(string Email);
        """;

    const string Slice = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Reactors;
        using Partners.Contracts;

        namespace Library.Admin.Invitations;

        public class AcceptedInvitationProvisioner : IReactor
        {
            public Task Provision(InvitationToJoinAdaAccepted @event, EventContext context) => Task.CompletedTask;
        }
        """;

    static readonly (string Path, string Text)[] _sources = [("Library/Admin/Invitations/Provision.cs", Slice)];

    MetadataReference _package;
    ApplicationModelAnalysis _analysis;

    void Establish() => _package = Analyzed.Package("Partners.Contracts", Contracts);

    void Because() => _analysis = Analyzed.SourceReferencing(_package, _sources);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_package, _sources).ShouldBeEmpty();
    [Fact] void should_import_the_event_by_its_qualified_name() => _analysis.Model.Imports.ShouldContainOnly(["Partners.Contracts.InvitationToJoinAdaAccepted"]);
    [Fact] void should_still_observe_it_from_the_reactor() => _analysis.Slice().Reactors.Single().ObservedEvents.ShouldContainOnly(["InvitationToJoinAdaAccepted"]);
    [Fact] void should_not_report_it_as_undeclared() => _analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.EventDeclaredOutsideCompilation).ShouldBeFalse();
    [Fact] void should_report_nothing_at_all() => _analysis.Diagnostics.ShouldBeEmpty();
}
