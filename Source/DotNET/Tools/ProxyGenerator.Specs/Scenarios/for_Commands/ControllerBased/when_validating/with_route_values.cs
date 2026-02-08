// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ControllerBased.when_validating;

[Collection(ScenarioCollectionDefinition.Name)]

public class with_route_values : given.a_scenario_web_application
{
    CommandResult<object>? _result;

    void Establish()
    {
        LoadControllerCommandProxy<ControllerCommandsController>(nameof(ControllerCommandsController.ExecuteValidatedWithRoute));
        ControllerCommandsController.ValidatedWithRouteCallCount = 0;
    }

    async Task Because()
    {
        var executionResult = await Bridge.ExecuteCommandViaProxyAsync<object>(new ControllerValidatedWithRouteCommand
        {
            Name = string.Empty,
            Value = -5
        },
            "ExecuteValidatedWithRoute");
        _result = executionResult.Result;
    }

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_validation_results() => _result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_have_name_validation_error() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("name"));
    [Fact] void should_have_name_validation_message() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("name") && v.Message == ControllerValidatedWithRouteCommandValidator.NameRequiredMessage);
    [Fact] void should_have_value_validation_error() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("value"));
    [Fact] void should_have_value_validation_message() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("value") && v.Message == ControllerValidatedWithRouteCommandValidator.ValueMinimumMessage);
    [Fact] void should_not_roundtrip_to_server() => ControllerCommandsController.ValidatedWithRouteCallCount.ShouldEqual(0);
}
