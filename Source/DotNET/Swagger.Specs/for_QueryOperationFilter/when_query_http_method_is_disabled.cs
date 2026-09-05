// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.OpenApi;

namespace Cratis.Arc.Swagger.for_QueryOperationFilter;

public class when_query_http_method_is_disabled : given.a_query_operation_filter
{
    OpenApiOperation _operation;

    void Establish()
    {
        _arcOptions.GeneratedApis.EnableQueryHttpMethod = false;

        var performer = CreateQueryPerformer("OrderById", supportsPaging: false);
        _queryPerformerProviders.Performers.Returns([performer]);

        _operation = CreateOperation("ExecuteOrderById");
    }

    void Because() => _filter.Apply(_operation, CreateFilterContext());

    [Fact] void should_not_add_a_query_http_method_note() => string.IsNullOrEmpty(_operation.Description).ShouldBeTrue();
}
