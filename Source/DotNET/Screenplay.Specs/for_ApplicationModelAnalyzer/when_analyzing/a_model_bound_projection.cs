// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A model-bound projection is declared upside down compared to a fluent one - the read model's properties say which
/// event they come from, rather than the event saying which properties it fills in. Regrouping them by event is what
/// turns the declaration back into the blocks a document is written in.
/// </summary>
public class a_model_bound_projection : Specification
{
    const string Source = """
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;

        namespace Library.Inventory.Listing;

        [EventType]
        public record BookAddedToInventory(string Title, int Count);

        [EventType]
        public record BookRemovedFromInventory(string Title);

        [ReadModel]
        [FromEvent<BookAddedToInventory>]
        [RemovedWith<BookRemovedFromInventory>]
        public record Book
        {
            [SetFrom<BookAddedToInventory>("title")]
            public string Title { get; init; } = string.Empty;

            [AddFrom<BookAddedToInventory>("count")]
            public int Available { get; init; }

            [Count<BookAddedToInventory>]
            public int Additions { get; init; }
        }
        """;

    ApplicationModelAnalysis _analysis;
    ProjectionModel _projection;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _projection = _analysis.Slice().Projections.Single();
    }

    ProjectionFromModel From => _projection.Scope.From.Single();

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_recover_a_projection() => _projection.ShouldNotBeNull();
    [Fact] void should_build_the_read_model_it_is_declared_on() => _projection.ReadModel.ShouldEqual("Book");
    [Fact] void should_map_automatically_by_default() => _projection.AutoMap.ShouldEqual(ProjectionAutoMapMode.Enabled);
    [Fact] void should_observe_the_event_the_read_model_names() => From.EventTypes.ShouldContainOnly(["BookAddedToInventory"]);
    [Fact] void should_map_a_property_from_the_event() => From.Properties["Title"].ShouldEqual("title");
    [Fact] void should_map_a_property_that_accumulates() => From.Properties["Available"].ShouldEqual("$add(count)");
    [Fact] void should_map_a_property_that_counts() => From.Properties["Additions"].ShouldEqual("$count");
    [Fact] void should_remove_the_instance_when_the_removing_event_occurs() => _projection.Scope.RemovedWith.Single().EventType.ShouldEqual("BookRemovedFromInventory");
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
