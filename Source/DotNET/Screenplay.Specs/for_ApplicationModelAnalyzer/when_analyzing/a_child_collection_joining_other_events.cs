// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

public class a_child_collection_joining_other_events : Specification
{
    ApplicationModelAnalysis _analysis;
    ProjectionChildScopeModel _child;

    void Establish()
    {
        _analysis = Analyzed.Source(ChildJoinSources.Source);
        _child = _analysis.Slice().Projections.Single().Scope.Children.Single();
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn((Analyzed.SlicePath, ChildJoinSources.Source)).ShouldBeEmpty();
    [Fact] void should_recover_both_joins() => _child.Scope.Joins.Count().ShouldEqual(2);
    [Fact] void should_join_on_the_child_identity() => _child.Scope.Joins.Select(join => join.On).Distinct().ShouldContainOnly(["codeId"]);
    [Fact] void should_keep_the_explicitly_mapped_property() => _child.Scope.Joins.Single(join => join.EventType == "CodeUnitObserved").Property.ShouldEqual("Unit");
    [Fact] void should_name_the_automatically_mapped_join() => _child.Scope.Joins.Single(join => join.EventType == "MonthOpened").Property.ShouldEqual("codeId");
    [Fact] void should_recover_the_context_mapping() => _child.Scope.From.Single().Properties["ApprovedBy"].ShouldEqual("$eventContext(causedBy.subject)");
    [Fact] void should_not_report_an_unmappable_join() => _analysis.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(ScreenplayDiagnosticCodes.UnmappableProjectionJoin);
}
