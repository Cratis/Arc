// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Reading the chain from the source is what makes a fluent projection expressible at all - at runtime it is an
/// expression tree that has already been compiled down to something a document cannot be recovered from.
/// </summary>
public class a_fluent_projection : Specification
{
    const string Source = """
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections;

        namespace Library.Authors.Listing;

        [EventType]
        public record AuthorRegistered(string Name);

        [EventType]
        public record AuthorRetired(string Name);

        [ReadModel]
        public record Author
        {
            public string Id { get; init; } = string.Empty;

            public string Name { get; init; } = string.Empty;

            public int Registrations { get; init; }
        }

        public class AuthorProjection : IProjectionFor<Author>
        {
            public void Define(IProjectionBuilderFor<Author> builder) => builder
                .AutoMap()
                .From<AuthorRegistered>(_ => _
                    .Set(m => m.Name).To(e => e.Name)
                    .Set(m => m.Id).ToEventSourceId()
                    .Increment(m => m.Registrations))
                .RemovedWith<AuthorRetired>();
        }
        """;

    ApplicationModelAnalysis _analysis;
    ProjectionModel _projection;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _projection = _analysis.Slice().Projection!;
    }

    ProjectionFromModel From => _projection.Scope.From.Single();

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_build_the_read_model_the_interface_names() => _projection.ReadModel.ShouldEqual("Author");
    [Fact] void should_recover_that_mapping_is_automatic() => _projection.AutoMap.ShouldEqual(ProjectionAutoMapMode.Enabled);
    [Fact] void should_observe_the_event_the_chain_names() => From.EventTypes.ShouldContainOnly(["AuthorRegistered"]);
    [Fact] void should_map_a_property_from_an_event_property() => From.Properties["Name"].ShouldEqual("name");
    [Fact] void should_map_a_property_from_the_event_source() => From.Properties["Id"].ShouldEqual("$eventSourceId");
    [Fact] void should_map_a_property_that_increments() => From.Properties["Registrations"].ShouldEqual("$increment");
    [Fact] void should_remove_the_instance_when_the_removing_event_occurs() => _projection.Scope.RemovedWith.Single().EventType.ShouldEqual("AuthorRetired");
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
