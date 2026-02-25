// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.OpenApi;

namespace Cratis.Arc.Swagger.for_QueryOperationFilter;

public class when_query_returns_single_result : given.a_query_operation_filter
{
    OpenApiOperation _operation;

    void Establish()
    {
        var performer = CreateQueryPerformer("OrderById", isEnumerable: false);
        _queryPerformerProviders.Performers.Returns([performer]);

        _operation = CreateOperation("ExecuteOrderById");
    }

    void Because() => _filter.Apply(_operation, CreateFilterContext());

    [Fact] void should_not_have_page_parameter() => _operation.Parameters.Any(p => p.Name == "page").ShouldBeFalse();
    [Fact] void should_not_have_page_size_parameter() => _operation.Parameters.Any(p => p.Name == "pageSize").ShouldBeFalse();
    [Fact] void should_not_have_sort_by_parameter() => _operation.Parameters.Any(p => p.Name == "sortby").ShouldBeFalse();
    [Fact] void should_not_have_sort_direction_parameter() => _operation.Parameters.Any(p => p.Name == "sortDirection").ShouldBeFalse();
    [Fact] void should_still_have_200_response() => _operation.Responses.ContainsKey("200").ShouldBeTrue();
}
