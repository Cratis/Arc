// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Projections;
using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Arc.Screenplay.for_ProjectionSyntaxBuilder.when_building;

public class a_child_collection_with_joins : Specification
{
    ScreenplayDiagnostics _diagnostics;
    ProjectionSyntax? _result;

    void Establish()
    {
        _diagnostics = new();
        var naming = new ScreenplayNaming();
        var builder = new ProjectionSyntaxBuilder(naming, _diagnostics, new NameAvailability(naming, _diagnostics));
        var scope = ProjectionScopeModel.Empty with
        {
            From = [new(["CodeObserved"], "codeId", null, Declare.Map(("unit", "unit")))],
            Joins = [new("unit", "CodeUnitObserved", "codeId", Declare.Map(("unit", "unit")))]
        };
        _result = builder.Build(
            new ProjectionModel("CodesProjection", "Codes", "event-log", ProjectionAutoMapMode.Enabled, false,
                ProjectionScopeModel.Empty with { Children = [new("entries", "codeId", ProjectionAutoMapMode.Inherit, scope)] }),
            "Library.Codes.Listing");
    }

    JoinSyntax Join => _result!.Blocks.OfType<ChildrenSyntax>().Single().Blocks.OfType<JoinSyntax>().Single();

    [Fact] void should_emit_the_join_inside_the_child_collection() => Join.On.ShouldEqual("codeId");
    [Fact] void should_not_report_an_unmappable_join() => _diagnostics.All.Select(diagnostic => diagnostic.Code).ShouldNotContain(ScreenplayDiagnosticCodes.UnmappableProjectionJoin);
}
