// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ControllerBased;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_executing_controller_command_that_throws_exception : given.a_scenario_web_application
{
    CommandResult<object>? _result;

    async Task Because()
    {
        var command = new ControllerExceptionCommand
        {
            ShouldThrow = true
        };

        var response = await HttpClient.PostAsJsonAsync("/api/controller-commands/exception", command);
        var json = await response.Content.ReadAsStringAsync();
        _result = System.Text.Json.JsonSerializer.Deserialize<CommandResult<object>>(json, Json.Globals.JsonSerializerOptions);
    }

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_exceptions() => _result.HasExceptions.ShouldBeTrue();
    [Fact] void should_have_exception_messages() => _result.ExceptionMessages.ShouldNotBeEmpty();
}
