// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.OpenApi;

namespace Cratis.Arc.Swagger.for_QueryResultOperationFilter;

public class when_response_has_no_json_content : given.a_query_result_operation_filter
{
    OpenApiOperation _operation;

    void Establish() => _operation = CreateOperation(new Dictionary<string, IOpenApiMediaType>());

    void Because() => _filter.Apply(_operation, CreateFilterContext(nameof(TestController.GetOne)));

    [Fact] void should_add_json_content_with_wrapper_schema() => _operation.Responses["200"].Content["application/json"].Schema.ShouldEqual(_schema);
    [Fact] void should_not_add_paging_parameters() => _operation.Parameters.Count.ShouldEqual(0);
}
