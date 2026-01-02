// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ControllerBased;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_executing_controller_simple_command : given.a_scenario_web_application
{
    CommandResult<object>? _result;

    async Task Because()
    {
        var command = new ControllerSimpleCommand
        {
            Name = "TestName",
            Count = 5
        };

        var response = await HttpClient.PostAsJsonAsync("/api/controller-commands/simple", command);
        var json = await response.Content.ReadAsStringAsync();
        _result = System.Text.Json.JsonSerializer.Deserialize<CommandResult<object>>(json, Json.Globals.JsonSerializerOptions);
    }

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
}
