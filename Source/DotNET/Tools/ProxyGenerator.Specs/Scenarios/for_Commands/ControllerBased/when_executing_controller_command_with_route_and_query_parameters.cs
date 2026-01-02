// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ControllerBased;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_executing_controller_command_with_route_and_query_parameters : given.a_scenario_web_application
{
    CommandResult<ControllerParameterResult>? _result;
    Guid _routeId;

    void Establish()
    {
        _routeId = Guid.NewGuid();
    }

    async Task Because()
    {
        var command = new ControllerParameterCommand
        {
            Value = "BodyValue"
        };

        var response = await HttpClient.PostAsJsonAsync($"/api/controller-commands/{_routeId}?filter=testFilter", command);
        var json = await response.Content.ReadAsStringAsync();
        _result = System.Text.Json.JsonSerializer.Deserialize<CommandResult<ControllerParameterResult>>(json, Json.Globals.JsonSerializerOptions);
    }

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_response() => _result.Response.ShouldNotBeNull();
    [Fact] void should_have_correct_route_id() => _result.Response.RouteId.ShouldEqual(_routeId);
    [Fact] void should_have_correct_query_filter() => _result.Response.QueryFilter.ShouldEqual("testFilter");
    [Fact] void should_have_correct_body_value() => _result.Response.BodyValue.ShouldEqual("BodyValue");
}
