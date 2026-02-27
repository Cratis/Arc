// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.OpenApi;

namespace Cratis.Arc.Swagger.for_CommandOperationFilter;

public class when_operation_matches_command_by_full_name : given.a_command_operation_filter
{
    OpenApiOperation _operation;
    ICommandHandler _handler;

    void Establish()
    {
        _handler = CreateCommandHandler(typeof(TestCommand));
        _commandHandlerProviders.Handlers.Returns([_handler]);

        // The CommandEndpointMapper uses FullName for operationId: $"Execute{handler.CommandType.FullName}"
        _operation = CreateOperation($"Execute{typeof(TestCommand).FullName}");
    }

    void Because() => _filter.Apply(_operation, CreateFilterContext());

    [Fact] void should_set_request_body() => _operation.RequestBody.ShouldNotBeNull();
    [Fact] void should_set_request_body_as_required() => _operation.RequestBody.Required.ShouldBeTrue();
    [Fact] void should_have_json_content_type() => _operation.RequestBody.Content.ContainsKey("application/json").ShouldBeTrue();
    [Fact] void should_have_200_response() => _operation.Responses.ContainsKey("200").ShouldBeTrue();
    [Fact] void should_have_400_response() => _operation.Responses.ContainsKey("400").ShouldBeTrue();
    [Fact] void should_have_403_response() => _operation.Responses.ContainsKey("403").ShouldBeTrue();
    [Fact] void should_have_500_response() => _operation.Responses.ContainsKey("500").ShouldBeTrue();
}
