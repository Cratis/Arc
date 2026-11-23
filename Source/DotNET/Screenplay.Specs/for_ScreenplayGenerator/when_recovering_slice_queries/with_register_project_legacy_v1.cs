// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_slice_queries;

public class with_register_project_legacy_v1 : a_generated_document
{
    const string Lookup = """
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections;
        using Cratis.Chronicle.ReadModels;
        using Cratis.Concepts;
        namespace Library.Registration.ProjectLookup;
        public record ProjectId(Guid Value) : EventSourceId<Guid>(Value);
        public record ProjectName(string Value) : ConceptAs<string>(Value);
        [EventType] public record ProjectRegistered(ProjectId ProjectId, ProjectName ProjectName);
        [ReadModel]
        public record ProjectSummary(ProjectId ProjectId, ProjectName ProjectName)
        {
            public static async Task<ProjectSummary?> ProjectById(IReadModels readModels, ProjectId projectId) =>
                await readModels.GetInstanceById<ProjectSummary>((EventSourceId)projectId);
        }
        public class ProjectSummaryProjection : IProjectionFor<ProjectSummary>
        {
            public void Define(IProjectionBuilderFor<ProjectSummary> builder) => builder
                .From<ProjectRegistered>(from => from
                    .Set(summary => summary.ProjectId).To(e => e.ProjectId)
                    .Set(summary => summary.ProjectName).To(e => e.ProjectName));
        }
        """;

    const string Other = """
        using System;
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.ReadModels;
        namespace Library.Registration.ProjectListing;
        [EventType] public record ProjectListed(Guid ListingId);
        [ReadModel]
        public record ProjectRow(Guid ListingId)
        {
            public static IEnumerable<ProjectRow> All(IReadModels readModels) => [];
        }
        """;

    QueryModel _analyzed;
    SemanticSlice _slice;
    SemanticKeyedQuery _bound;
    SemanticReadModel _summary;

    void Because()
    {
        Generate(
            ("Library/Program.cs", "namespace Library;"),
            ("Library/Registration/ProjectLookup/ProjectLookup.cs", Lookup),
            ("Library/Registration/ProjectListing/ProjectListing.cs", Other));
        _analyzed = Result.Model.Slices.Single(slice => slice.Name == "ProjectLookup").Queries.Single();
        _slice = Bound.Value.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "ProjectLookup");
        _bound = _slice.Queries.Single();
        _summary = _slice.ReadModels.Single(model => model.Name == "ProjectSummary");
    }

    [Fact] void should_recover_the_query_in_its_owning_slice() => _analyzed.Name.ShouldEqual("ProjectById");
    [Fact] void should_recover_the_key_as_the_nominal_concept() => _analyzed.By.Type.ShouldEqual(new TypeReferenceModel("ProjectId", false, false));
    [Fact] void should_recover_the_read_model_as_an_optional_single_result() => _analyzed.ReturnType.ShouldEqual(new TypeReferenceModel("ProjectSummary", false, true));
    [Fact] void should_not_expose_the_read_models_service_as_an_argument() => _analyzed.Filters.ShouldBeEmpty();
    [Fact] void should_leave_the_other_slice_with_its_own_query() => Result.Model.Slices.Single(slice => slice.Name == "ProjectListing").Queries.Select(query => query.Name).ShouldContainOnly("All");
    [Fact] void should_say_the_key_as_the_concept() => Result.Source.ShouldContain("by projectId ProjectId");
    [Fact] void should_bind_exactly_the_one_query() => _slice.Queries.Length.ShouldEqual(1);
    [Fact] void should_bind_the_query_name() => _bound.Name.ShouldEqual("ProjectById");
    [Fact] void should_bind_the_query_to_the_summary() => _bound.ReadModel.ShouldEqual(_summary.Id);
    [Fact] void should_bind_the_argument_to_the_declared_concept() => (_bound.Argument.Name, _bound.Argument.Type).ShouldEqual(("projectId", SemanticTypeReference.ForConcept(Bound.Value.Model.Application.Concepts.Single(concept => concept.Name == "ProjectId").Id)));
    [Fact] void should_bind_the_key_property() => _summary.Properties.Single(property => property.Id == _bound.KeyProperty).Name.ShouldEqual("projectId");
    [Fact] void should_bind_a_single_snapshot_result() => (_bound.Cardinality, _bound.Delivery).ShouldEqual((SemanticQueryCardinality.ZeroOrOne, SemanticQueryDelivery.Snapshot));
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
