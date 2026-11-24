// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.OpenApi;

namespace Cratis.Arc.Swagger.for_CommandResultOperationFilter;

public class when_response_has_json_content_with_metadata : given.a_command_result_operation_filter
{
    OpenApiOperation _operation;
    OpenApiMediaType _original;

    void Establish()
    {
        _original = new OpenApiMediaType { Schema = new OpenApiSchema { Type = JsonSchemaType.String }, Example = Example };
        _operation = CreateOperation(new Dictionary<string, IOpenApiMediaType> { ["application/json"] = _original });
    }

    void Because() => _filter.Apply(_operation, CreateFilterContext(nameof(TestController.Execute)));

    [Fact] void should_set_the_wrapper_schema() => _operation.Responses["200"].Content["application/json"].Schema.ShouldEqual(_schema);
    [Fact] void should_preserve_the_example() => _operation.Responses["200"].Content["application/json"].Example.ShouldEqual(_original.Example);
    [Fact] void should_add_forbidden_response_with_wrapper() => _operation.Responses["403"].Content["application/json"].Schema.ShouldEqual(_schema);
    [Fact] void should_add_bad_request_response_with_wrapper() => _operation.Responses["400"].Content["application/json"].Schema.ShouldEqual(_schema);
    [Fact] void should_add_internal_server_error_response_with_wrapper() => _operation.Responses["500"].Content["application/json"].Schema.ShouldEqual(_schema);
}
