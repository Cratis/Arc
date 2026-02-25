// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.OpenApi;

namespace Cratis.Arc.Swagger.for_QueryOperationFilter;

public class when_query_returns_enumerable_result : given.a_query_operation_filter
{
    OpenApiOperation _operation;

    void Establish()
    {
        var performer = CreateQueryPerformer("AllOrders", isEnumerable: true);
        _queryPerformerProviders.Performers.Returns([performer]);

        _operation = CreateOperation("ExecuteAllOrders");
    }

    void Because() => _filter.Apply(_operation, CreateFilterContext());

    [Fact] void should_have_page_parameter() => _operation.Parameters.Any(p => p.Name == "page").ShouldBeTrue();
    [Fact] void should_have_page_size_parameter() => _operation.Parameters.Any(p => p.Name == "pageSize").ShouldBeTrue();
    [Fact] void should_have_sort_by_parameter() => _operation.Parameters.Any(p => p.Name == "sortby").ShouldBeTrue();
    [Fact] void should_have_sort_direction_parameter() => _operation.Parameters.Any(p => p.Name == "sortDirection").ShouldBeTrue();
    [Fact] void should_have_200_response() => _operation.Responses.ContainsKey("200").ShouldBeTrue();
    [Fact] void should_have_400_response() => _operation.Responses.ContainsKey("400").ShouldBeTrue();
    [Fact] void should_have_403_response() => _operation.Responses.ContainsKey("403").ShouldBeTrue();
    [Fact] void should_have_500_response() => _operation.Responses.ContainsKey("500").ShouldBeTrue();
}
