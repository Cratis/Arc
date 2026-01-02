// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ControllerBased;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_executing_controller_command_with_validation_errors_produced_by_the_client : given.a_scenario_web_application
{
    CommandResult<object>? _result;

    void Establish()
    {
        LoadControllerCommandProxy<ControllerCommandsController>(nameof(ControllerCommandsController.ExecuteFluentValidated));
        ControllerCommandsController.FluentValidatedCallCount = 0;
    }

    async Task Because()
    {
        var executionResult = await Bridge.ExecuteCommandViaProxyAsync<object>(new ControllerFluentValidatedCommand
        {
            Title = string.Empty,
            Quantity = -5,
            Email = "invalid-email"
        },
            "ExecuteFluentValidated");
        _result = executionResult.Result;
    }

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_validation_results() => _result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_have_title_validation_error() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("title"));
    [Fact] void should_have_title_validation_message() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("title") && v.Message == ControllerFluentValidatedCommandValidator.TitleRequiredMessage);
    [Fact] void should_have_quantity_validation_error() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("quantity"));
    [Fact] void should_have_quantity_validation_message() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("quantity") && v.Message == ControllerFluentValidatedCommandValidator.QuantityMinimumMessage);
    [Fact] void should_have_email_validation_error() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("email"));
    [Fact] void should_have_email_validation_message() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("email") && v.Message == ControllerFluentValidatedCommandValidator.EmailRequiredMessage);
    [Fact] void should_not_roundtrip_to_server() => ControllerCommandsController.FluentValidatedCallCount.ShouldEqual(0);
}
