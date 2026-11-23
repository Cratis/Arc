// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_slice_queries;

public class with_esm_golden_v28 : a_generated_document
{
    const string Lookup = """
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Keys;
        using Cratis.Chronicle.Projections;
        using Cratis.Chronicle.ReadModels;
        using Cratis.Concepts;
        namespace Library.F.Lookup;
        public record MemberId(Guid Value) : ConceptAs<Guid>(Value);
        [EventType] public record MemberAssigned(string Id, MemberId MemberId, MemberId? Owner);
        [ReadModel]
        public record View([Key] string Id, MemberId MemberId, MemberId? Owner)
        {
            public static async Task<View?> Get(IReadModels readModels, EventSourceId id) =>
                await readModels.GetInstanceById<View>(id);
        }
        public class ViewProjection : IProjectionFor<View>
        {
            public void Define(IProjectionBuilderFor<View> builder) => builder
                .From<MemberAssigned>(from => from
                    .Set(view => view.Id).To(e => e.Id)
                    .Set(view => view.MemberId).To(e => e.MemberId)
                    .Set(view => view.Owner).To(e => e.Owner));
        }
        """;

    const string Write = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        namespace Library.F.Write;
        [EventType] public record Recorded(string Id);
        [Command]
        public record Record(string Id)
        {
            public Recorded Handle() => new(Id);
        }
        """;

    QueryModel _analyzed;
    SemanticSlice _slice;
    SemanticKeyedQuery _bound;
    SemanticReadModel _view;

    void Because()
    {
        Generate(
            ("Library/Program.cs", "namespace Library;"),
            ("Library/F/Lookup/Lookup.cs", Lookup),
            ("Library/F/Write/Write.cs", Write));
        _analyzed = Result.Model.Slices.Single(slice => slice.Name == "Lookup").Queries.Single();
        _slice = Bound.Value.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Lookup");
        _bound = _slice.Queries.Single();
        _view = _slice.ReadModels.Single(model => model.Name == "View");
    }

    [Fact] void should_recover_the_query_in_its_owning_slice() => _analyzed.Name.ShouldEqual("Get");
    [Fact] void should_recover_the_key_as_a_string() => _analyzed.By.Type.ShouldEqual(new TypeReferenceModel("String", false, false));
    [Fact] void should_recover_the_read_model_as_an_optional_single_result() => _analyzed.ReturnType.ShouldEqual(new TypeReferenceModel("View", false, true));
    [Fact] void should_not_expose_the_read_models_service_as_an_argument() => _analyzed.Filters.ShouldBeEmpty();
    [Fact] void should_not_place_the_query_in_the_write_slice() => Result.Model.Slices.Single(slice => slice.Name == "Write").Queries.ShouldBeEmpty();
    [Fact] void should_say_the_key_as_a_string() => Result.Source.ShouldContain("by id String");
    [Fact] void should_bind_exactly_the_one_query() => _slice.Queries.Length.ShouldEqual(1);
    [Fact] void should_bind_the_query_name() => _bound.Name.ShouldEqual("Get");
    [Fact] void should_bind_the_query_to_the_view() => _bound.ReadModel.ShouldEqual(_view.Id);
    [Fact] void should_bind_the_argument_as_a_string() => (_bound.Argument.Name, _bound.Argument.Type).ShouldEqual(("id", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text)));
    [Fact] void should_bind_the_key_property() => _view.Properties.Single(property => property.Id == _bound.KeyProperty).Name.ShouldEqual("id");
    [Fact] void should_bind_a_single_snapshot_result() => (_bound.Cardinality, _bound.Delivery).ShouldEqual((SemanticQueryCardinality.ZeroOrOne, SemanticQueryDelivery.Snapshot));
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
