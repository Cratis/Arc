// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.OpenApi;

namespace Cratis.Arc.Swagger.for_CommandOperationFilter;

public class when_operation_id_does_not_start_with_execute : given.a_command_operation_filter
{
    OpenApiOperation _operation;

    void Establish()
    {
        var handler = CreateCommandHandler(typeof(TestCommand));
        _commandHandlerProviders.Handlers.Returns([handler]);

        _operation = CreateOperation("ValidateSomething");
    }

    void Because() => _filter.Apply(_operation, CreateFilterContext());

    [Fact] void should_not_set_request_body() => _operation.RequestBody.ShouldBeNull();
    [Fact] void should_not_add_error_responses() => _operation.Responses.Count.ShouldEqual(0);
}
