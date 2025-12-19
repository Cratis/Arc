// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ControllerBased;

public class when_executing_controller_command_with_result : given.a_scenario_web_application
{
    CommandResult<ControllerCommandResult>? _result;

    async Task Because()
    {
        var command = new ControllerCommandWithResult
        {
            Input = "TestInput",
            RetryDelay = TimeSpan.FromSeconds(15)
        };

        var response = await HttpClient.PostAsJsonAsync("/api/controller-commands/with-result", command);
        var json = await response.Content.ReadAsStringAsync();
        _result = System.Text.Json.JsonSerializer.Deserialize<CommandResult<ControllerCommandResult>>(json, Json.Globals.JsonSerializerOptions);
    }

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_response() => _result.Response.ShouldNotBeNull();
    [Fact] void should_have_correct_message() => _result.Response.Message.ShouldContain("Received: TestInput");
    [Fact] void should_have_correct_retry_delay() => _result.Response.ReceivedRetryDelay.ShouldEqual(TimeSpan.FromSeconds(15));
}
