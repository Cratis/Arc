// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Commands;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ConcurrencySyntaxBuilder.when_reporting_the_event_source;

/// <summary>
/// The language has no way to declare an event source or to bind a command to one, so a command that appends through
/// a definition must say so rather than read as a command appending to the default stream.
/// </summary>
public class a_command_appending_through_an_event_source : Specification
{
    ScreenplayDiagnostics _diagnostics;
    ConcurrencySyntaxBuilder _builder;

    void Establish()
    {
        _diagnostics = new();
        _builder = new(new ScreenplayNaming(), _diagnostics);
    }

    void Because()
    {
        _builder.ReportEventSource(new EventSourceBindingModel("Account", "Transactions", true, true), "Library.Accounts.Deposit");
        _builder.ReportEventSource(new EventSourceBindingModel("Account", "Missing", false, false), "Library.Accounts.Misplace");
        _builder.ReportEventSource(null, "Library.Accounts.Legacy");
    }

    [Fact]
    void should_not_suggest_enabling_authoring_when_it_is_already_enabled()
    {
        var diagnostics = new ScreenplayDiagnostics();
        new ConcurrencySyntaxBuilder(new ScreenplayNaming(), diagnostics).ReportEventSource(new("Account", "Transactions", true, false), "Library.Accounts.Deposit", true);
        diagnostics.All.Single().Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
        diagnostics.All.Single().Message.ShouldContain("no unambiguous readable route");
        diagnostics.All.Single().Message.Contains("enable", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact] void should_report_what_cannot_be_stated() => _diagnostics.All.Select(_ => _.Code).ShouldContainOnly(
        ScreenplayDiagnosticCodes.EventSourceNotRepresentable,
        ScreenplayDiagnosticCodes.EventStreamIdConcurrencyNotRepresentable,
        ScreenplayDiagnosticCodes.EventStreamNotDeclared,
        ScreenplayDiagnosticCodes.EventSourceNotRepresentable);
    [Fact] void should_report_disabled_authoring_as_information() => _diagnostics.All.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EventSourceNotRepresentable).All(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Information).ShouldBeTrue();
    [Fact] void should_name_the_event_source_and_stream() => _diagnostics.All.First(_ => _.Code == ScreenplayDiagnosticCodes.EventSourceNotRepresentable).Message.ShouldContain("event source 'Account' stream 'Transactions'");
    [Fact] void should_report_nothing_for_a_command_without_an_event_source() => _diagnostics.All.Count(_ => _.Location == "Library.Accounts.Legacy").ShouldEqual(0);
}
