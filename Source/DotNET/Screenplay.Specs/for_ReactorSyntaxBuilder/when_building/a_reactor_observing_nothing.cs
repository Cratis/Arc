// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Reactors;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ReactorSyntaxBuilder.when_building;

/// <summary>
/// A reactor declaration with no triggers has an empty body and does not compile.
/// </summary>
public class a_reactor_observing_nothing : Specification
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
        new ReactorModel("ReservationNotifier", [], false, null),
        "Library.Lending.Notifications");

    [Fact] void should_emit_no_declaration() => _result.ShouldBeNull();
    [Fact] void should_report_the_reactor() => _diagnostics.All.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.ReactorWithoutEvents]);
}
