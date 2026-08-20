// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Reactions;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ReactionSyntaxBuilder.when_building;

/// <summary>
/// An artifact living in a referenced package has metadata but no source, so no path can be read off a syntax tree.
/// A trigger with neither a file nor an inline body has an empty body and does not compile, so the vertical slice
/// convention is used to point somewhere rather than nowhere.
/// </summary>
public class a_reactor_without_a_source_file : Specification
{
    ScreenplayDiagnostics _diagnostics;
    ReactionSyntaxBuilder _builder;
    ReactionSyntax? _result;

    void Establish()
    {
        _diagnostics = new();
        _builder = new(new ScreenplayNaming(), _diagnostics);
    }

    void Because() => _result = _builder.Build(
        new ReactorModel("RestockRequester", ["BookReserved"], true, null),
        "Library.Lending.Restocking");

    [Fact] void should_point_at_the_conventional_path() => _result!.Triggers.Single().File!.Path.ShouldEqual("Lending/Restocking/RestockRequester.cs");
    [Fact] void should_observe_the_event() => ((NamedTriggerSourceSyntax)_result!.Triggers.Single().Source).Name.ShouldEqual("BookReserved");
    [Fact] void should_report_nothing() => _diagnostics.All.ShouldBeEmpty();
}
