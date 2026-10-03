// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Reactors;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ReactorSyntaxBuilder.when_building;

/// <summary>
/// The published Screenplay syntax cannot narrow a reaction to an event source or stream. The reaction is still
/// emitted, and the filter is reported rather than silently dropped.
/// </summary>
public class a_reactor_filtered_to_an_event_source : Specification
{
    ScreenplayDiagnostics _diagnostics;
    ReactorSyntaxBuilder _builder;
    ReactionSyntax? _result;

    void Establish()
    {
        _diagnostics = new();
        _builder = new(new ScreenplayNaming(), _diagnostics);
    }

    void Because() => _result = _builder.Build(
        new ReactorModel("Notifier", ["FundsDeposited"], false, null, new ObservedEventSourceModel("Account", "Missing", false)),
        "Library.Accounts");

    [Fact] void should_still_emit_the_reaction() => _result!.Triggers.Count().ShouldEqual(1);
    [Fact] void should_report_the_filter_it_cannot_state() => _diagnostics.All.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.ObserverEventSourceNotRepresentable);
    [Fact] void should_report_the_undeclared_stream() => _diagnostics.All.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.ObserverEventStreamNotDeclared);
}
