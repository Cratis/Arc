// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A type holding itself describes a hierarchy of no fixed depth, which a document writes out one level at a time
/// and therefore cannot express. The level that is expressible is kept and the rest is reported, because a reader
/// that followed the declaration would never come back.
/// </summary>
public class a_child_collection_of_the_type_holding_it : Specification
{
    const string Source = """
        using System;
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;

        namespace Library.Modeling.Slices;

        [EventType]
        public record SliceAdded(Guid SliceId, Guid ParentSliceId, string Name);

        [ReadModel]
        [FromEvent<SliceAdded>]
        public record Slice(
            Guid Id,
            string Name,
            [ChildrenFrom<SliceAdded>(
                key: nameof(SliceAdded.SliceId),
                parentKey: nameof(SliceAdded.ParentSliceId))]
            IEnumerable<Slice> Children);
        """;

    ApplicationModelAnalysis _analysis;
    Model.ProjectionChildScopeModel _children;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _children = _analysis.Slice().Projection!.Scope.Children.Single();
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_keep_the_level_it_can_express() => _children.Scope.From.Single().Key.ShouldEqual("sliceId");
    [Fact] void should_stop_before_holding_itself_again() => _children.Scope.Children.ShouldBeEmpty();
    [Fact] void should_report_what_it_left_out() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.UnmappableProjectionScope);
    [Fact] void should_say_a_document_cannot_nest_without_end() => _analysis.Diagnostics.Any(_ => _.Message.Contains("cannot nest without end", StringComparison.Ordinal)).ShouldBeTrue();
}
