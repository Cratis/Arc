// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.OpenApi;

namespace Cratis.Arc.Swagger.for_QueryOperationFilter;

public class when_operation_does_not_match_any_query : given.a_query_operation_filter
{
    OpenApiOperation _operation;

    void Establish()
    {
        var performer = CreateQueryPerformer("AllOrders", supportsPaging: true);
        _queryPerformerProviders.Performers.Returns([performer]);

        _operation = CreateOperation("ExecuteNonExistentQuery");
    }

    void Because() => _filter.Apply(_operation, CreateFilterContext());

    [Fact] void should_not_add_any_parameters() => _operation.Parameters.Count.ShouldEqual(0);
    [Fact] void should_not_add_any_responses() => _operation.Responses.Count.ShouldEqual(0);
}
