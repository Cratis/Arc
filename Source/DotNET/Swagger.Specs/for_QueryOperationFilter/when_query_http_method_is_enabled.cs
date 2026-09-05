// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.OpenApi;

namespace Cratis.Arc.Swagger.for_QueryOperationFilter;

public class when_query_http_method_is_enabled : given.a_query_operation_filter
{
    OpenApiOperation _operation;

    void Establish()
    {
        _arcOptions.GeneratedApis.EnableQueryHttpMethod = true;

        var performer = CreateQueryPerformer("OrderById", supportsPaging: false);
        _queryPerformerProviders.Performers.Returns([performer]);

        _operation = CreateOperation("ExecuteOrderById");
    }

    void Because() => _filter.Apply(_operation, CreateFilterContext());

    [Fact] void should_note_the_query_http_method_in_the_description() => _operation.Description!.Contains("HTTP QUERY method").ShouldBeTrue();
}
