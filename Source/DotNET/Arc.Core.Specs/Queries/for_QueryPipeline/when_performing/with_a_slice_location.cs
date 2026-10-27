// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Slices;
using Cratis.Arc.Slices.for_SliceLocation.Billing.Invoices.Pay;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_a_slice_location : given.a_query_pipeline
{
    QueryContext _context = null!;

    void Establish()
    {
        var name = new FullyQualifiedQueryName("Test.SliceLocation");
        _queryPerformer.Type.Returns(typeof(PayInvoice));
        _queryPerformer.Dependencies.Returns([]);
        _queryPerformerProviders.TryGetPerformersFor(name, out var _).Returns(call =>
        {
            call[1] = _queryPerformer;
            return true;
        });
        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(call =>
        {
            _context = call.Arg<QueryContext>();
            return QueryResult.Success(CorrelationId.New());
        });
    }

    async Task Because() => await _pipeline.Perform("Test.SliceLocation", QueryArguments.Empty, Paging.NotPaged, Sorting.None, _serviceProvider);

    [Fact] void should_put_the_slice_location_of_the_query_on_the_query_context() => _context.SliceLocation.ShouldEqual(new SliceLocation("Billing", "Invoices", "Pay"));
}
