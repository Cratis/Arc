// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_slice_queries;

public class with_read_model_absence_v5 : a_generated_document
{
    const string Lookup = """
        using System.Threading.Tasks;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Keys;
        using Cratis.Chronicle.Projections;
        using Cratis.Chronicle.ReadModels;
        namespace Library.Invoices.InvoiceLookup;
        [EventType] public record InvoiceRaised(string InvoiceId);
        [EventType] public record InvoiceRemoved();
        [ReadModel]
        public record InvoiceView([Key] string InvoiceId)
        {
            public static async Task<InvoiceView?> InvoiceById(IReadModels readModels, EventSourceId invoiceId) =>
                await readModels.GetInstanceById<InvoiceView>(invoiceId);
        }
        public class InvoiceViewProjection : IProjectionFor<InvoiceView>
        {
            public void Define(IProjectionBuilderFor<InvoiceView> builder) => builder
                .From<InvoiceRaised>(from => from.Set(view => view.InvoiceId).To(e => e.InvoiceId))
                .RemovedWith<InvoiceRemoved>();
        }
        """;

    const string Other = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.ReadModels;
        namespace Library.Invoices.InvoiceListing;
        [EventType] public record InvoiceListed(string RowId);
        [ReadModel]
        public record InvoiceRow(string RowId)
        {
            public static IEnumerable<InvoiceRow> All(IReadModels readModels) => [];
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
            ("Library/Invoices/InvoiceLookup/InvoiceLookup.cs", Lookup),
            ("Library/Invoices/InvoiceListing/InvoiceListing.cs", Other));
        _analyzed = Result.Model.Slices.Single(slice => slice.Name == "InvoiceLookup").Queries.Single();
        var slices = Bound.Value.Model.Application.Modules.Single().Features.Single().Slices;
        _slice = slices.Single(slice => slice.Name == "InvoiceLookup");
        _bound = _slice.Queries.Single();
        _view = _slice.ReadModels.Single(model => model.Name == "InvoiceView");
    }

    [Fact] void should_recover_the_query_in_its_owning_slice() => _analyzed.Name.ShouldEqual("InvoiceById");
    [Fact] void should_recover_the_key_as_a_string() => _analyzed.By.Type.ShouldEqual(new TypeReferenceModel("String", false, false));
    [Fact] void should_recover_the_read_model_as_an_optional_single_result() => _analyzed.ReturnType.ShouldEqual(new TypeReferenceModel("InvoiceView", false, true));
    [Fact] void should_not_expose_the_read_models_service_as_an_argument() => _analyzed.Filters.ShouldBeEmpty();
    [Fact] void should_leave_the_other_slice_with_its_own_query() => Result.Model.Slices.Single(slice => slice.Name == "InvoiceListing").Queries.Select(query => query.Name).ShouldContainOnly("All");
    [Fact] void should_say_the_key_as_a_string() => Result.Source.ShouldContain("by invoiceId String");
    [Fact] void should_bind_exactly_the_one_query() => _slice.Queries.Length.ShouldEqual(1);
    [Fact] void should_bind_the_query_name() => _bound.Name.ShouldEqual("InvoiceById");
    [Fact] void should_bind_the_query_to_the_view() => _bound.ReadModel.ShouldEqual(_view.Id);
    [Fact] void should_bind_the_argument_as_a_string() => (_bound.Argument.Name, _bound.Argument.Type).ShouldEqual(("invoiceId", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text)));
    [Fact] void should_bind_the_key_property() => _view.Properties.Single(property => property.Id == _bound.KeyProperty).Name.ShouldEqual("invoiceId");
    [Fact] void should_bind_a_single_snapshot_result() => (_bound.Cardinality, _bound.Delivery).ShouldEqual((SemanticQueryCardinality.ZeroOrOne, SemanticQueryDelivery.Snapshot));
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
