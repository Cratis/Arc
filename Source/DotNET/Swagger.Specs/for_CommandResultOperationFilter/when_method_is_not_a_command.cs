// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.OpenApi;

namespace Cratis.Arc.Swagger.for_CommandResultOperationFilter;

public class when_method_is_not_a_command : given.a_command_result_operation_filter
{
    OpenApiOperation _operation;

    void Establish() => _operation = CreateOperation(new Dictionary<string, IOpenApiMediaType>());

    void Because() => _filter.Apply(_operation, CreateFilterContext(nameof(TestController.NotACommand)));

    [Fact] void should_not_add_content() => _operation.Responses["200"].Content.Count.ShouldEqual(0);
    [Fact] void should_not_add_error_responses() => _operation.Responses.Count.ShouldEqual(1);
}
