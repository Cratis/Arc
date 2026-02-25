// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Microsoft.OpenApi;

namespace Cratis.Arc.Swagger.for_QueryOperationFilter;

public class when_query_has_custom_parameters : given.a_query_operation_filter
{
    OpenApiOperation _operation;

    void Establish()
    {
        var performer = CreateQueryPerformer(
            "OrdersByCustomer",
            supportsPaging: true,
            new QueryParameter("customerId", typeof(string)),
            new QueryParameter("status", typeof(int)));

        _queryPerformerProviders.Performers.Returns([performer]);
        _operation = CreateOperation("ExecuteOrdersByCustomer");
    }

    void Because() => _filter.Apply(_operation, CreateFilterContext());

    [Fact] void should_have_customer_id_parameter() => _operation.Parameters.Any(p => p.Name == "customerId").ShouldBeTrue();
    [Fact] void should_have_status_parameter() => _operation.Parameters.Any(p => p.Name == "status").ShouldBeTrue();
    [Fact] void should_also_have_paging_parameters() => _operation.Parameters.Any(p => p.Name == "page").ShouldBeTrue();
    [Fact] void should_have_six_parameters_total() => _operation.Parameters.Count.ShouldEqual(6);
}
