// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ControllerBased;

public class when_executing_controller_abstract_validated_command_with_validation_errors : given.a_scenario_web_application
{
    CommandResult<object>? _result;

    void Establish() => ControllerCommandsController.AbstractValidatedCallCount = 0;

    async Task Because()
    {
        var command = new ControllerAbstractValidatedCommand
        {
            Code = "ABC",
            Amount = -100
        };

        var response = await HttpClient.PostAsJsonAsync("/api/controller-commands/abstract-validated", command);
        var json = await response.Content.ReadAsStringAsync();
        _result = System.Text.Json.JsonSerializer.Deserialize<CommandResult<object>>(json, Json.Globals.JsonSerializerOptions);
    }

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_validation_results() => _result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_have_code_validation_error() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("Code"));
    [Fact] void should_have_amount_validation_error() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("Amount"));
    [Fact] void should_not_roundtrip_to_server() => ControllerCommandsController.AbstractValidatedCallCount.ShouldEqual(0);
}
